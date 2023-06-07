using System;
using System.Runtime.InteropServices;

namespace AStyleExtension
{
    public class AStyleInterface
    {
        // --------------------------------------------------------------------------------
        internal static class NativeMethods
        {
            internal delegate void AStyleErrorDelegate(int errorNum, [MarshalAs(UnmanagedType.LPStr)] string error);
            internal delegate IntPtr AStyleMemAllocDelegate(int size);

            [DllImport("AStyle/AStyle.dll", CharSet = CharSet.Unicode)]
            internal static extern IntPtr AStyleMainUtf16(
                [MarshalAs(UnmanagedType.LPWStr)] string sIn,
                [MarshalAs(UnmanagedType.LPWStr)] string sOptions,
                AStyleErrorDelegate errorFunc,
                AStyleMemAllocDelegate memAllocFunc
            );
        }
        // --------------------------------------------------------------------------------

        public delegate void AStyleErrorHandler(object sender, AStyleErrorArgs e);
        public event AStyleErrorHandler ErrorRaised;

        private readonly NativeMethods.AStyleErrorDelegate _aStyleError;
        private readonly NativeMethods.AStyleMemAllocDelegate _aStyleMemAlloc;

        public AStyleInterface()
        {
            _aStyleMemAlloc = this.OnAStyleMemAlloc;
            _aStyleError = this.OnAStyleError;
        }

        /// Call the AStyleMain function in Artistic Style.
        /// An empty string is returned on error.
        public string FormatSource(string textIn, string options)
        {
            // Return the allocated string
            // Memory space is allocated by OnAStyleMemAlloc, a callback function
            var sTextOut = string.Empty;

            try
            {
                var pText = NativeMethods.AStyleMainUtf16(textIn, options, _aStyleError, _aStyleMemAlloc);

                if (pText != IntPtr.Zero)
                {
                    sTextOut = Marshal.PtrToStringUni(pText);
                    Marshal.FreeHGlobal(pText);
                }
            }
            catch (Exception ex)
            {
                string msg = $"an error has occurred. : Source = {ex.Source}\n" +
                             $"-> [Message] {ex.Message}\n" +
                             $"-> [TargetSite] {ex.TargetSite.ToString()}\n" +
                             string.Format("**** StackTrace ****\n{0}", ex.StackTrace
                                           .Replace("   場所", "\n場所")
                                           .Replace("  場所", "\n場所")
                                           .Replace(" 場所", "\n場所")
                                           .Replace("   at ", "\nat ")
                                           .Replace("  at ", "\nat ")
                                           .Replace(" at ", "\nat ")
                                           .Replace("\n\n", "\n"));
                this.OnAStyleError(this, new AStyleErrorArgs(msg));
            }

            return sTextOut;
        }

        // Allocate the memory for the Artistic Style return string.
        private IntPtr OnAStyleMemAlloc(int size)
        {
            return Marshal.AllocHGlobal(size);
        }

        private void OnAStyleError(object source, AStyleErrorArgs args)
        {
            var tmp = this.ErrorRaised;
            tmp?.Invoke(source, args);
        }

        private void OnAStyleError(int errorNumber, string errorMessage)
        {
            this.OnAStyleError(this, new AStyleErrorArgs(errorNumber + ": " + errorMessage));
        }
    }
}