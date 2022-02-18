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
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Mastersign.AutoForm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void InitializeHighlighting()
        {
            var t = typeof(MainWindow);
            var a = t.Assembly;
            using (var s = a.GetManifestResourceStream(t.Namespace + ".YAML-Mode.xshd"))
            {
                using (var r = new XmlTextReader(s))
                {
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(r, HighlightingManager.Instance);
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeHighlighting();
        }
    }
}
