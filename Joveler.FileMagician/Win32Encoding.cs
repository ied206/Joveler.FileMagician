using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

[assembly: InternalsVisibleTo("Joveler.FileMagician.Tests")]
namespace Joveler.FileMagician
{
    internal static class Win32Encoding
    {
        #region Const
        private const int CP_ACP = 0;
        #endregion

        #region IsActiveCodePageCompatible
        public static unsafe bool IsActiveCodePageCompatible(string str)
        {
#if !NET451
            // Assume non-Windows platforms such as linux always use UTF-8
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true;
#endif

            // Get required buffer size
            int bufferSize = NativeMethods.Win32.WideCharToMultiByte(CP_ACP, 0, str, -1, null, 0, null, null);

            // Try to convert unicode string to multi-byte, and see whether conversion fails or not.
            // WC_ERR_INVALID_CHARS flag mandates conversion to fail if wide-char string contains incompatible character in system codepage.
            // https://docs.microsoft.com/en-us/windows/desktop/api/stringapiset/nf-stringapiset-widechartomultibyte
            bool lpUsedDefaultChar = false;
            byte[] buffer = new byte[bufferSize + 2];
            int ret = NativeMethods.Win32.WideCharToMultiByte(CP_ACP, 0, str, -1, buffer, bufferSize, null, &lpUsedDefaultChar);

            // Return test result
            if (ret == 0)
                return false; // Conversion failed, assume that str is not compatible
            return !lpUsedDefaultChar;
        }
        #endregion
    }
}
