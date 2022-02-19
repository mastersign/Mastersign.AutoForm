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
            textEditor.Text = EXAMPLE;
        }

        private const string EXAMPLE = @"# This is a YAML document
title: Dark Theme Demo
theme: 'dark'
grid:
  # comments
  columns: 4
  rows:5
'defaultSlot': main # comments
array:
- 'abc'
- ABC
- true
- 123
- 127.0.0.1
slots:
  main: { columnSpan: 4, rowSpan: '3','history': ""20}"", ""extensions"":50 } # CMT
  A:
    visible: yes
    row: 3
    ""column Span"":'wide'
    'rowSpan': null
    colorFilter: saturate(30%) contrast(400%) brightness(50%)
  B: {
    row: 3,
    column: 'auto',
    columnSpan: 2, # CMT
    rowSpan: 2,
    extensions: [ ""a{"", 'b', {x: 123}, ]
    extensions2: [""a,"",'b',{'x':123},]
  }
docstring: |
  ABC
  DEF

'something else': >+3
   This
   https://boomack.com
   is more text.

# THE END
";
    }
}
