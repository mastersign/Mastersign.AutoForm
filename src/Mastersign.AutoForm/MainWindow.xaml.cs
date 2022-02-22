using System;
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

        public MainWindow()
        {
            InitializeComponent();
            Runner = new ScriptRunner();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Runner.Initialize(null);
            tBrowserWarning.Visibility = Runner.IsReady ? Visibility.Collapsed : Visibility.Visible;
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
            txtLog.Text = Project.ToString();
            txtLog.Foreground = Project.HasErrors ? Brushes.Maroon : SystemColors.ControlTextBrush;
            btnRun.IsEnabled = !Project.HasErrors;
            iconOK.Visibility = Project.HasErrors ? Visibility.Hidden : Visibility.Visible;
            iconError.Visibility = Project.HasErrors ? Visibility.Visible : Visibility.Hidden;
            lblProjectName.Content = Project.Name;
            lblProjectDescription.Content = Project.Description;
            var viewportWidth = Project.ViewportWidth.HasValue ? Project.ViewportWidth.ToString() : "auto";
            var viewportHeight = Project.ViewportHeight.HasValue ? Project.ViewportHeight.ToString() : "auto";
            lblViewport.Content = $"{viewportWidth} ⨉ {viewportHeight} px";
            lblActions.Content = Project.Actions.Count.ToString();
            lblSkippedActions.Content = Project.SkippedActions.ToString();
            if (Project.HasErrors)
            {
                txtErrors.Text = string.Join("\n", Project.Errors);
            }
            else
            {
                txtErrors.Text = string.Empty;
                await Runner.Initialize(Project);
            }
            btnReload.IsEnabled = true;
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
            if (Project == null || Project.HasErrors || Project.Actions.Count == 0) return;

            try
            {
                btnChooseExcelFile.IsEnabled = false;
                btnReload.IsEnabled = false;
                btnRun.IsEnabled = false;

                await Runner.Initialize(Project);
                foreach (var action in Project.Actions)
                {
                    if (action is PauseAction pauseAction)
                    {
                        var result = MessageBox.Show(this, pauseAction.Label, "AutoForm Pause",
                            MessageBoxButton.OKCancel, MessageBoxImage.Information);
                        if (result == MessageBoxResult.Cancel) break;
                    }
                    else
                    {
                        await Runner.Run(action);
                    }
                }
                MessageBox.Show(this, "Automation finished.", "AutoForm Finish",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}
