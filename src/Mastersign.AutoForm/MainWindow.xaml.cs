﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace Mastersign.AutoForm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AutomationProject Project { get; set; }

        private ScriptRunner Runner { get; }
        private bool IsIncapable { get; set; }
        private int? RecordNumber { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Title = "Mastersign AutoForm v" + GetType().Assembly.GetName().Version.ToString(3);
            Runner = new ScriptRunner();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await Runner.Initialize(null);
                IsIncapable = !Runner.IsReady;
            }
            catch (NotSupportedException)
            {
                IsIncapable = true;
            }
            tBrowserWarning.Visibility = IsIncapable ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnDownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDlg = new SaveFileDialog
            {
                Title = "Save AutoForm Template...",
                AddExtension = true,
                Filter = "Excel Worksheets (*.xlsx)|*.xlsx",
                OverwritePrompt = true,
            };
            if (saveFileDlg.ShowDialog() == true)
            {
                var t = typeof(MainWindow);
                var a = t.Assembly;
                using (var src = a.GetManifestResourceStream(t.Namespace + ".Template.xlsx"))
                using (var dst = File.OpenWrite(saveFileDlg.FileName))
                {
                    src.CopyTo(dst);
                }
            }
        }

        private async void LoadAutomationProject(string filename)
        {
            txtExcelFile.Text = filename;
            var factory = new AutomationProjectFactory();
            Project = factory.ParseExcelFile(filename);
            tLog.Text = Project.ToString();
            tLog.Foreground = Project.HasErrors ? Brushes.Maroon : SystemColors.ControlTextBrush;
            btnRun.IsEnabled = !Project.HasErrors && !IsIncapable;
            iconOK.Visibility = Project.HasErrors ? Visibility.Hidden : Visibility.Visible;
            iconError.Visibility = Project.HasErrors ? Visibility.Visible : Visibility.Hidden;
            lblProjectName.Content = Project.Name;
            lblProjectDescription.Content = Project.Description;
            var viewportWidth = Project.ViewportWidth.HasValue ? Project.ViewportWidth.ToString() : "auto";
            var viewportHeight = Project.ViewportHeight.HasValue ? Project.ViewportHeight.ToString() : "auto";
            lblViewport.Content = $"{viewportWidth} ⨉ {viewportHeight} px";
            lblActions.Content = Project.PreActions.Count + " / " + Project.LoopActions.Count + " / " + Project.PostActions.Count;
            lblSkippedActions.Content = Project.SkippedActions.ToString();
            if (Project.Records.Count > 0)
            {
                lblRecords.Content = Project.Records.Count.ToString();
                gridRecordControl.Visibility = Visibility.Visible;
                RecordNumber = 0;
            }
            else
            {
                lblRecords.Content = "none";
                gridRecordControl.Visibility = Visibility.Collapsed;
                RecordNumber = null;
            }
            UpdateRecordUI();
            if (Project.HasErrors)
            {
                txtErrors.Text = string.Join("\n", Project.Errors);
            }
            else
            {
                txtErrors.Text = string.Empty;
                tabItemRecordPreview.Visibility = Project.Records.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                await Runner.Initialize(Project);
            }
            btnReload.IsEnabled = true;
        }

        private void UpdateRecordUI()
        {
            if (RecordNumber.HasValue)
            {
                gridRecordControl.IsEnabled = true;
                var no = RecordNumber.Value;
                lblRecordNumber.Text = $"{no + 1} of {Project.Records.Count}";
                btnRecordFirst.IsEnabled = no > 0;
                btnRecordPrevious.IsEnabled = no > 0;
                btnRecordNext.IsEnabled = no < Project.Records.Count - 1;
                btnRecordLast.IsEnabled = no < Project.Records.Count - 1;
                chkOnlyCurrent.IsEnabled = true;
                lstRecords.ItemsSource = Project.RecordColumns
                    .Select(col => new KeyValuePair<string, object>(
                        col, Project.Records[RecordNumber.Value][col].GetValue()));
            }
            else
            {
                gridRecordControl.IsEnabled = false;
                lblRecordNumber.Text = "—";
                btnRecordFirst.IsEnabled = false;
                btnRecordPrevious.IsEnabled = false;
                btnRecordNext.IsEnabled = false;
                btnRecordLast.IsEnabled = false;
                chkOnlyCurrent.IsEnabled = false;
                lstRecords.ItemsSource = null;
            }
        }

        private void btnChooseExcelFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDlg = new OpenFileDialog
            {
                Title = "Open AutoForm Script...",
                Filter = "Excel Worksheets (*.xlsx)|*.xlsx",
                Multiselect = false,
            };
            if (openFileDlg.ShowDialog() == true)
            {
                LoadAutomationProject(openFileDlg.FileName);
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtExcelFile.Text)) return;
            LoadAutomationProject(txtExcelFile.Text);
        }

        private async void btnRun_Click(object sender, RoutedEventArgs e)
        {
            if (Project == null || Project.HasErrors) return;
            if (Project.PreActions.Count == 0 &&
                Project.LoopActions.Count == 0 &&
                Project.PostActions.Count == 0) return;

            try
            {
                btnChooseExcelFile.IsEnabled = false;
                btnReload.IsEnabled = false;
                btnRun.IsEnabled = false;
                var withoutCancelling = false;

                await Runner.Initialize(Project);
                if (Project.Records.Count == 0 || chkOnlyCurrent.IsChecked == true)
                {
                    withoutCancelling =
                        await ExecutePreActions()
                        && await ExecuteLoopActions()
                        && await ExecutePostActions();
                }
                else
                {
                    withoutCancelling = await ExecutePreActions();
                    if (withoutCancelling)
                    {
                        for (var i = 0; i < Project.Records.Count; i++)
                        {
                            RecordNumber = i;
                            UpdateRecordUI();
                            withoutCancelling = await ExecuteLoopActions();
                            if (!withoutCancelling) break;
                        }
                    }
                    if (withoutCancelling)
                    {
                        withoutCancelling = await ExecutePostActions();
                    }
                }

                if (withoutCancelling)
                {
                    MessageBox.Show(this, "Automation finished.", "AutoForm Finish",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(this, "Automation was cancelled.", "AutoForm Cancelled",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error during execution.\n\n" + ex.Message, "AutoForm Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnRun.IsEnabled = true;
                btnReload.IsEnabled = true;
                btnChooseExcelFile.IsEnabled = true;
            }
        }

        private async Task<bool> ExecutePreActions()
        {
            foreach (var action in Project.PreActions)
            {
                if (!await ExecuteAction(action)) return false;
            }
            return true;
        }

        private async Task<bool> ExecuteLoopActions()
        {
            if (RecordNumber.HasValue) tabs.SelectedItem = tabItemRecordPreview;
            foreach (var action in Project.LoopActions)
            {
                if (!await ExecuteLoopAction(action)) return false;
            }
            return true;
        }

        private async Task<bool> ExecutePostActions()
        {
            foreach (var action in Project.PostActions)
            {
                if (!await ExecuteAction(action)) return false;
            }
            return true;
        }

        private async Task<bool> ExecuteAction(Action a)
        {
            if (a.DeactivatedByCondition) return true;
            if (a is PauseAction pauseAction)
            {
                if (chkNoPause.IsChecked == true) return true;
                var result = MessageBox.Show(this, pauseAction.Label, "AutoForm Pause",
                    MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.Cancel) return false;
            }
            else
            {
                await Runner.Run(a);
            }
            return true;
        }

        private async Task<bool> ExecuteLoopAction(Action a)
        {
            if (RecordNumber.HasValue)
            {
                var record = Project.Records[RecordNumber.Value];
                try
                {
                    a = a.Substitute(record);
                }
                catch (SubstitutionException e)
                {
                    var result = MessageBox.Show(this,
                        "Substitution failed: " + e.Message + "\n\n" +
                        "If you press OK, the current record is skipped." +
                        "Otherwise the execution is canceled.",
                        "AutoForm Substitution Error",
                        MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.OK)
                        return true;
                    else
                        return false;
                }
            }
            return await ExecuteAction(a);
        }

        private void btnRecordFirst_Click(object sender, RoutedEventArgs e)
        {
            if (Project == null || Project.Records.Count == 0)
                RecordNumber = null;
            else
                RecordNumber = 0;
            UpdateRecordUI();
        }

        private void btnRecordPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (Project == null || Project.Records.Count == 0)
                RecordNumber = null;
            else if (RecordNumber == null)
                RecordNumber = Project.Records.Count - 1;
            else
                RecordNumber = Math.Max(0, RecordNumber.Value - 1);
            UpdateRecordUI();
        }

        private void btnRecordNext_Click(object sender, RoutedEventArgs e)
        {
            if (Project == null || Project.Records.Count == 0)
                RecordNumber = null;
            else if (RecordNumber == null)
                RecordNumber = 0;
            else
                RecordNumber = Math.Min(Project.Records.Count - 1, RecordNumber.Value + 1);
            UpdateRecordUI();
        }

        private void btnRecordLast_Click(object sender, RoutedEventArgs e)
        {
            if (Project == null || Project.Records.Count == 0)
                RecordNumber = null;
            else
                RecordNumber = Project.Records.Count - 1;
            UpdateRecordUI();
        }
    }
}
