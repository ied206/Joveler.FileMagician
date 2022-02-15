/*
    C# pinvoke wrapper written by Hajin Jang
    Copyright (C) 2022 Hajin Jang

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

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Joveler.FileMagician
{
    internal unsafe class Helper
    {
        #region Temp Path
        private static readonly object TempPathLock = new();
        private static readonly RandomNumberGenerator SecureRandom = RandomNumberGenerator.Create();

        public static string GetTempDir()
        {
            lock (TempPathLock)
            {
                byte[] randBytes = new byte[4];
                string systemTempDir = Path.GetTempPath();

                string tempDir;
                do
                {
                    // Get 4B of random 
                    SecureRandom.GetBytes(randBytes);
                    uint randInt = BitConverter.ToUInt32(randBytes, 0);

                    tempDir = Path.Combine(systemTempDir, $"Jovel_FileMagic_{randInt:X8}");
                }
                while (Directory.Exists(tempDir) || File.Exists(tempDir));

                // Create base temp directory
                Directory.CreateDirectory(tempDir);

                return tempDir;
            }
        }
        #endregion

        #region IsAnsiCompatible
        /// <summary>
        /// Check if the given string is compatible with system's active ANSI codepage.
        /// </summary>
        /// <remarks>
        /// Same functionality can be implemented with Encoding and EncoderFallback, but it involves exception throwing.
        /// </remarks>
        public static unsafe bool IsActiveCodePageCompatible(string str)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                return true;

            return IsCodePageCompatible(NativeMethods.CP_ACP, str);
        }

        /// <summary>
        /// Check if the given string is compatible with a given codepage.
        /// </summary>
        /// <remarks>
        /// Same functionality can be implemented with Encoding and EncoderFallback, but it involves exception throwing.
        /// </remarks>
        public static unsafe bool IsCodePageCompatible(uint codepage, string str)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                return true;

            // Empty string must be compatible to any encoding, right?
            if (str.Length == 0)
                return true;

            // Get required buffer size
            int bufferSize = NativeMethods.WideCharToMultiByte(codepage, 0, str, -1, null, 0, null, null);

            // Try to convert unicode string to multi-byte, and see whether conversion fails or not.
            int usedDefaultChar = 0;
            byte[] buffer = new byte[bufferSize + 2];
            int ret = NativeMethods.WideCharToMultiByte(codepage, NativeMethods.WC_NO_BEST_FIT_CHARS, str, -1, buffer, bufferSize, null, &usedDefaultChar);

            // Return test result
            if (ret == 0)
                return false; // Conversion failed, assume that str is not compatible
            return usedDefaultChar == 0;
        }
        #endregion
    }
}
