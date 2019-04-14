using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class TestSetup
    {
        public static string MagicFile = "magic.mgc";
        public static string ExeDir;
        public static string BaseDir;
        public static string SampleDir;

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            ExeDir = TestHelper.GetProgramAbsolutePath();
            BaseDir = Path.GetFullPath(Path.Combine(ExeDir, "..", "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");

            const string x64 = "x64";
            const string x86 = "x86";
            const string armhf = "armhf";
            const string arm64 = "arm64";

            const string dllName = "libmagic-1.dll";
            const string soName = "libmagic.so";

            string libPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        libPath = Path.Combine(x86, dllName);
                        break;
                    case Architecture.X64:
                        libPath = Path.Combine(x64, dllName);
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        libPath = Path.Combine(x64, soName);
                        break;
                    case Architecture.Arm:
                        libPath = Path.Combine(armhf, soName);
                        break;
                    case Architecture.Arm64:
                        libPath = Path.Combine(arm64, soName);
                        break;
                }
            }

            if (libPath == null)
                throw new PlatformNotSupportedException();

            Magic.GlobalInit(libPath);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            Magic.GlobalCleanup();
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
