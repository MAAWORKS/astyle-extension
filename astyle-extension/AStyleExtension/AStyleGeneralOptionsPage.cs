using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace AStyleExtension
{
    [CLSCompliant(false), ComVisible(true)]
    public class AStyleGeneralOptionsPage : DialogPage
    {
        private AStyleGeneralOptionsControl _control;

        public string CppOptions
        {
            get; set;
        }
        public string CsOptions
        {
            get; set;
        }
        public bool CppFormatOnSave
        {
            get; set;
        }
        public bool CsFormatOnSave
        {
            get; set;
        }
        public bool IsCSarpEnabled
        {
            get; set;
        }

        public AStyleGeneralOptionsPage()
        {
            this.IsCSarpEnabled = true;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override IWin32Window Window
        {
            get
            {
                _control = new AStyleGeneralOptionsControl();

                if (this.CppOptions != null)
                {
                    _control.CppOptions = this.CppOptions;
                }

                if (this.CsOptions != null)
                {
                    _control.CsOptions = this.CsOptions;
                }

                _control.IsCSarpEnabled = this.IsCSarpEnabled;
                _control.CppFormatOnSave = this.CppFormatOnSave;
                _control.CsFormatOnSave = this.CsFormatOnSave;

                return _control;
            }
        }

        protected override void OnDeactivate(CancelEventArgs e)
        {
            if (_control != null)
            {
                this.CppOptions = _control.CppOptions;
                this.CsOptions = _control.CsOptions;
                this.CppFormatOnSave = _control.CppFormatOnSave;
                this.CsFormatOnSave = _control.CsFormatOnSave;
            }

            base.OnDeactivate(e);
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            if (_control != null)
            {
                _control.CppOptions = this.CppOptions;
                _control.CsOptions = this.CsOptions;
                _control.CppFormatOnSave = this.CppFormatOnSave;
                _control.CsFormatOnSave = this.CsFormatOnSave;

                _control.ClearDetails();
            }

            base.OnActivate(e);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (_control != null)
            {
                this.CppOptions = _control.CppOptions;
                this.CsOptions = _control.CsOptions;
                this.CppFormatOnSave = _control.CppFormatOnSave;
                this.CsFormatOnSave = _control.CsFormatOnSave;
            }

            base.OnApply(e);
        }
    }
}
