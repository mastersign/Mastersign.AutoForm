using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using ICSharpCode.TextEditor.Document;

namespace Mastersign.AutoForm
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            HighlightingManager.Manager.AddSyntaxModeFileProvider(new CustomSyntaxModeProvider());
            
            // textEditor.Document.HighlightingStrategy = hs;
            textEditor.Highlighting = "XML";
        }
    }

    class CustomSyntaxModeProvider : ISyntaxModeFileProvider
    {
        private static readonly SyntaxMode JSON_MODE
            = new SyntaxMode("JSON-Mode.xshd", "XML", ".json");

        public ICollection<SyntaxMode> SyntaxModes 
            => new[] { JSON_MODE };

        public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
        {
            var t = typeof(CustomSyntaxModeProvider);
            var stream = t.Assembly.GetManifestResourceStream(
                t.Namespace + "." + syntaxMode.FileName);
            return new XmlTextReader(stream);
        }

        public void UpdateSyntaxModeList()
        {
            // noop
        }
    }
}
