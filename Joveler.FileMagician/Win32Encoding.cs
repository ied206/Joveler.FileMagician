/*
    C# pinvoke wrapper written by Hajin Jang
    Copyright (C) 2019 Hajin Jang

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:

    1. Redistributions of source code must retain the above copyright 
       notice, this list of conditions and the following disclaimer.
   
    2. Redistributions in binary form must reproduce the above copyright
       notice, this list of conditions and the following disclaimer in the
       documentation and/or other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
    AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
    ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
    LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
    SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
    INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
    CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
    ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
    THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Runtime.CompilerServices;
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
