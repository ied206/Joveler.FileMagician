using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
                throw new PlatformNotSupportedException();

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
