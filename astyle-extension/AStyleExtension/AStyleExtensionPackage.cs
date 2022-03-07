using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.AsyncPackageHelpers;
using Microsoft.VisualStudio.Shell;

namespace AStyleExtension
{
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [InstalledProductRegistration("#110", "#112", "3.1", IconResourceID = 400)]
    [AsyncPackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, Microsoft.VisualStudio.AsyncPackageHelpers.PackageAutoLoadFlags.BackgroundLoad)]
    [Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, Microsoft.VisualStudio.AsyncPackageHelpers.PackageAutoLoadFlags.BackgroundLoad)]
    [Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, Microsoft.VisualStudio.AsyncPackageHelpers.PackageAutoLoadFlags.BackgroundLoad)]
    [Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, Microsoft.VisualStudio.AsyncPackageHelpers.PackageAutoLoadFlags.BackgroundLoad)]

    [Guid(GuidList.GuidPkgString)]
    [ProvideOptionPage(typeof(AStyleGeneralOptionsPage), "AStyle Formatter", "General", 1000, 1001, true)]
    public sealed class AStyleExtensionPackage : Package
    {
        private DTE _dte;
        private OleMenuCommand _formatSelMenuCommand;
        private OleMenuCommand _formatDocMenuCommand;
        private bool _isCSharpEnabled;
        private AStyleGeneralOptionsPage _dialog;
        private DocumentEventListener _documentEventListener;

        protected override void Initialize()
        {
            base.Initialize();

            if (this.GetService(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
            {
                var id = new CommandID(GuidList.GuidCmdSet, (int)PkgCmdIDList.FormatDocumentCommand);
                _formatDocMenuCommand = new OleMenuCommand(this.FormatDocumentCallback, id);
                mcs.AddCommand(_formatDocMenuCommand);
                _formatDocMenuCommand.BeforeQueryStatus += this.OnBeforeQueryStatus;

                id = new CommandID(GuidList.GuidCmdSet, (int)PkgCmdIDList.FormatSelectionCommand);
                _formatSelMenuCommand = new OleMenuCommand(this.FormatSelectionCallback, id);
                mcs.AddCommand(_formatSelMenuCommand);
                _formatSelMenuCommand.BeforeQueryStatus += this.OnBeforeQueryStatus;
            }

            _dte = (DTE)this.GetService(typeof(DTE));

            _documentEventListener = new DocumentEventListener(this);
            _documentEventListener.BeforeSave += this.OnBeforeDocumentSave;

            if (_dte.RegistryRoot.Contains("VisualStudio"))
            {
                _isCSharpEnabled = true;
            }

            _dialog = (AStyleGeneralOptionsPage)this.GetDialogPage(typeof(AStyleGeneralOptionsPage));
            _dialog.IsCSarpEnabled = _isCSharpEnabled;
        }

        private TextDocument GetTextDocument(Document doc)
        {
            if (doc == null || doc.ReadOnly)
            {
                return null;
            }

            var textDoc = doc.Object("TextDocument") as TextDocument;

            return textDoc;
        }

        private Language GetLanguage(Document doc)
        {
            var language = Language.NA;
            var lang = doc.Language.ToLower();

            if (lang == "gcc" || lang == "avrgcc" || lang == "c/c++")
            {
                language = Language.Cpp;
            }
            else if (lang == "csharp" && _isCSharpEnabled)
            {
                language = Language.CSharp;
            }

            return language;
        }

        private int OnBeforeDocumentSave(uint docCookie)
        {
            if (!_dialog.CppFormatOnSave && !_dialog.CsFormatOnSave)
            {
                return VSConstants.S_OK;
            }

            var doc = _dte.Documents.OfType<Document>().FirstOrDefault(x => x.FullName == _documentEventListener.GetDocumentName(docCookie));
            var language = this.GetLanguage(doc);

            if (language == Language.CSharp && _dialog.CsFormatOnSave)
            {
                this.FormatDocument(this.GetTextDocument(doc), Language.CSharp);
            }
            else if (language == Language.Cpp && _dialog.CppFormatOnSave)
            {
                this.FormatDocument(this.GetTextDocument(doc), Language.Cpp);
            }

            return VSConstants.S_OK;
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            var language = this.GetLanguage(_dte.ActiveDocument);

            if (language != Language.CSharp && language != Language.Cpp)
            {
                cmd.Visible = false;
            }
            else
            {
                cmd.Visible = true;
            }

            cmd.Enabled = cmd.Visible;
        }

        private void FormatDocumentCallback(object sender, EventArgs e)
        {
            var language = this.GetLanguage(_dte.ActiveDocument);
            var textDoc = this.GetTextDocument(_dte.ActiveDocument);

            this.FormatDocument(textDoc, language);
        }

        private void FormatDocument(TextDocument textDoc, Language language)
        {
            if (textDoc == null || language == Language.NA)
            {
                return;
            }

            var sp = textDoc.StartPoint.CreateEditPoint();
            var ep = textDoc.EndPoint.CreateEditPoint();
            var text = sp.GetText(ep);

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var formattedText = this.Format(text, language);

            if (!string.IsNullOrEmpty(formattedText))
            {
                sp.ReplaceText(ep, formattedText, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
            }
        }

        private void FormatSelectionCallback(object sender, EventArgs e)
        {
            var language = this.GetLanguage(_dte.ActiveDocument);
            var textDoc = this.GetTextDocument(_dte.ActiveDocument);

            this.FormatSelection(textDoc, language);
        }

        private void FormatSelection(TextDocument textDoc, Language language)
        {
            if (textDoc == null || language == Language.NA)
            {
                return;
            }

            var newLineReplacement = "";

            if (textDoc.Selection.IsEmpty)
            {
                return;
            }

            var sp = textDoc.Selection.TopPoint.CreateEditPoint();
            var ep = textDoc.Selection.BottomPoint.CreateEditPoint();

            var text = textDoc.Selection.Text;

            var pos = 0;

            foreach (var c in text)
            {
                pos++;

                if (c != ' ' && c != '\t')
                {
                    break;
                }
            }

            if (pos > 0)
            {
                newLineReplacement = text.Substring(0, pos - 1);
            }

            var formattedText = this.Format(text, language);

            if (!string.IsNullOrEmpty(newLineReplacement))
            {
                var lines = Regex.Split(formattedText, "\r\n|\r|\n");

                for (var x = 0; x < lines.Length; x++)
                {
                    if (!string.IsNullOrEmpty(lines[x]))
                    {
                        lines[x] = newLineReplacement + lines[x];
                    }
                }

                formattedText = string.Join(Environment.NewLine, lines);
            }

            if (!string.IsNullOrEmpty(formattedText))
            {
                sp.ReplaceText(ep, formattedText, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
            }
        }

        private string Format(string text, Language language)
        {
            string options;

            if (_dialog == null)
            {
                MessageBox.Show("Unable to read AStyle Formatter settings.", "AStyle Formatter Error");
                return null;
            }

            switch (language)
            {
                case Language.CSharp:
                    options = _dialog.CsOptions;
                    break;

                case Language.Cpp:
                    options = _dialog.CppOptions;
                    break;

                default:
                    return null;
            }

            if (string.IsNullOrEmpty(options))
            {
                return null;
            }

            var aStyle = new AStyleInterface();
            aStyle.ErrorRaised += this.OnAStyleErrorRaised;

            return aStyle.FormatSource(text, options);
        }

        private void OnAStyleErrorRaised(object source, AStyleErrorArgs args)
        {
            MessageBox.Show(args.Message, "AStyle Formatter Error");
        }
    }
}
