using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.FileMagician
{
    internal class NativeMethods
    {
        #region EncodingHelper
        public const uint CP_ACP = 0;
        public const uint WC_NO_BEST_FIT_CHARS = 0x00000400;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int GetACP();

        [DllImport("kernel32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern unsafe int WideCharToMultiByte(
            uint codePage,
            uint dwFlags,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWideCharStr,
            int cchWideChar,
            [MarshalAs(UnmanagedType.LPArray)] byte[] lpMultiByteStr,
            int cbMultiByte,
            byte* lpDefaultChar,
            int* lpUsedDefaultChar);
        #endregion
    }
}
