/*
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    [SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable")]
    public class TestSetup
    {
        public static string ExeDir;
        public static string BaseDir;
        public static string SampleDir;
        private static string MagicTempDir;

        public static string MagicFile = "magic.mgc";
        // Force .NET's unicode to ansi encoding convert failure by using exotic/obscure characters in path
        public static string MagicUnicodeOnlyPath = "ᄒᆞᆫ글ḀḘ韓國.mgc";

        [AssemblyInitialize]
        [SuppressMessage("Style", "IDE0060")]
        public static void Init(TestContext context)
        {
            ExeDir = TestHelper.GetProgramAbsolutePath();
            BaseDir = Path.GetFullPath(Path.Combine(ExeDir, "..", "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");
            MagicTempDir = TestHelper.GetTempDir();

            MagicFile = Path.Combine(ExeDir, MagicFile);
            MagicUnicodeOnlyPath = Path.Combine(MagicTempDir, MagicUnicodeOnlyPath);
            File.Copy(MagicFile, MagicUnicodeOnlyPath, true);

            string libPath = GetNativeLibPath();
            Magic.GlobalInit(libPath);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            Magic.GlobalCleanup();
            if (Directory.Exists(MagicTempDir))
                Directory.Delete(MagicTempDir, true);
        }

        private static string GetNativeLibPath()
        {
            string libDir = string.Empty;

#if !NET48
            libDir = @"runtimes";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libDir = Path.Combine(libDir, "win-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libDir = Path.Combine(libDir, "linux-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libDir = Path.Combine(libDir, "osx-");
#endif

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    libDir += "x86";
                    break;
                case Architecture.X64:
                    libDir += "x64";
                    break;
                case Architecture.Arm:
                    libDir += "arm";
                    break;
                case Architecture.Arm64:
                    libDir += "arm64";
                    break;
            }

#if NETCOREAPP2_1
            libDir = Path.Combine(libDir, "native", "netstandard2.0");
#elif NETCOREAPP3_1
            libDir = Path.Combine(libDir, "native", "netstandard2.1");
#endif

            string libPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libPath = Path.Combine(libDir, "libmagic-1.dll");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libPath = Path.Combine(libDir, "libmagic.so");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libPath = Path.Combine(libDir, "libmagic.dylib");

            if (libPath == null)
                throw new PlatformNotSupportedException($"Unable to find native library.");
            if (!File.Exists(libPath))
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

            return libPath;
        }
    }

    #region Helper
    public static class TestHelper
    {
        #region File and Path
        public static string GetProgramAbsolutePath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (Path.GetDirectoryName(path) != null)
                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return path;
        }

        private static readonly object TempDirLock = new object();
        public static string GetTempDir()
        {
            lock (TempDirLock)
            {
                string path = Path.GetTempFileName();
                File.Delete(path);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string NormalizePath(string str)
        {
            char[] newStr = new char[str.Length];
            for (int i = 0; i < newStr.Length; i++)
            {
                switch (str[i])
                {
                    case '\\':
                    case '/':
                        newStr[i] = Path.DirectorySeparatorChar;
                        break;
                    default:
                        newStr[i] = str[i];
                        break;
                }
            }
            return new string(newStr);
        }

        public static string[] NormalizePaths(IEnumerable<string> strs)
        {
            return strs.Select(NormalizePath).ToArray();
        }

        public static Tuple<string, bool>[] NormalizePaths(IEnumerable<Tuple<string, bool>> tuples)
        {
            return tuples.Select(x => new Tuple<string, bool>(NormalizePath(x.Item1), x.Item2)).ToArray();
        }
        #endregion
    }
    #endregion
}
