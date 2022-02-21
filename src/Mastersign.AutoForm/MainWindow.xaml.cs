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

        public MainWindow()
        {
            InitializeComponent();
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

        private void LoadAutomationProject(string filename)
        {
            txtExcelFile.Text = filename;
            var factory = new AutomationProjectFactory();
            Project = factory.ParseExcelFile(filename);
            txtLog.Text = Project.ToString();
            txtLog.Foreground = Project.HasErrors ? Brushes.Maroon : SystemColors.ControlTextBrush;
            btnRun.IsEnabled = !Project.HasErrors;
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

            var runner = new ScriptRunner();
            try
            {
                await runner.Initialize();
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
                        await runner.Run(action);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error during execution.\n\n" + ex.Message, "AutoForm Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                runner.Dispose();
            }
        }
    }
}
