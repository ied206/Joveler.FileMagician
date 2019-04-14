﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace Joveler.FileMagician
{
    public class Magic : IDisposable
    {
        #region Const
        #endregion

        #region Field
        private IntPtr _ptr;
        #endregion

        #region Constructor (private)
        private Magic(IntPtr ptr)
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            _ptr = ptr;
        }
        #endregion

        #region Disposable Pattern
        ~Magic()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            if (_ptr == IntPtr.Zero)
                return;

            NativeMethods.MagicClose(_ptr);
            _ptr = IntPtr.Zero;
        }
        #endregion

        #region (Static) GlobalInit, GlobalCleanup
        public static void GlobalInit(string libPath)
        {
            if (NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgAlreadyInited);

#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                if (libPath == null || !File.Exists(libPath))
                    throw new ArgumentException("Specified .dll file does not exist");

                libPath = Path.GetFullPath(libPath);

                // libmagic itself depends on bunch of libraries.
                // LoadLibrary fails if libmagic-1.dll is not in the standard Windows' dll lookup path.
                // To fix this, setup dll search directory when necessary.
                string libDir = Path.GetDirectoryName(libPath);
                if (libDir != null && !libDir.Equals(AppDomain.CurrentDomain.BaseDirectory))
                    NativeMethods.Win32.SetDllDirectory(libDir);

                NativeMethods.hModule = NativeMethods.Win32.LoadLibrary(libPath);

                // Reset dll search directory to prevent dll hijacking
                NativeMethods.Win32.SetDllDirectory(null);

                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());

                // Check if dll is valid (libmagic-1.dll)
                if (NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, "magic_open") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid libmagic-1.dll");
                }
            }
#if !NET451
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (libPath == null)
                    libPath = "/usr/lib/x86_64-linux-gnu/libmagic.so.1"; // Try to call system-installed libmagic
                if (!File.Exists(libPath))
                    throw new ArgumentException("Specified .so file does not exist");

                NativeMethods.hModule = NativeMethods.Linux.dlopen(libPath, NativeMethods.Linux.RTLD_NOW | NativeMethods.Linux.RTLD_GLOBAL);
                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}], {NativeMethods.Linux.dlerror()}");

                // Check if dll is valid libmagic.so
                if (NativeMethods.Linux.dlsym(NativeMethods.hModule, "magic_open") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid libmagic-1.so");
                }
            }
#endif

            try
            {
                NativeMethods.LoadFunctions();
            }
            catch (Exception)
            {
                GlobalCleanup();
                throw;
            }
        }

        public static void GlobalCleanup()
        {
            if (NativeMethods.Loaded)
            {
                NativeMethods.ResetFunctions();
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    int ret = NativeMethods.Win32.FreeLibrary(NativeMethods.hModule);
                    Debug.Assert(ret != 0);
                }
#if !NET451
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    int ret = NativeMethods.Linux.dlclose(NativeMethods.hModule);
                    Debug.Assert(ret == 0);
                }
#endif
                NativeMethods.hModule = IntPtr.Zero;
            }
            else
            {
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);
            }
        }
        #endregion

        #region (Static) Open
        public static Magic Open(MagicFlags flags = MagicFlags.NONE)
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            IntPtr ptr = NativeMethods.MagicOpen(flags);
            if (ptr == null)
                throw new InvalidOperationException("Can't create magic");

            return new Magic(ptr);
        }

        public static Magic Open(string magicFile, MagicFlags flags = MagicFlags.NONE)
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            IntPtr ptr = NativeMethods.MagicOpen(flags);
            if (ptr == null)
                throw new InvalidOperationException("Can't create magic");

            Magic magic = new Magic(ptr);
            magic.Load(magicFile);
            return magic;
        }
        #endregion

        #region (Static) Magic File Path

        /// <summary>
        /// Get default (or given) path of magicFile.
        /// </summary>
        public static string GetPath(string magicFile, bool autoLoad = true)
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            IntPtr magicFilePtr = Marshal.StringToHGlobalAnsi(magicFile);
            try
            {
                IntPtr strPtr = NativeMethods.MagicGetPath(magicFilePtr, autoLoad ? 0 : -1);
                return Marshal.PtrToStringAnsi(strPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(magicFilePtr);
            }
        }
        #endregion

        #region Check Type
        public string CheckFile(string inName)
        {
            IntPtr strPtr = NativeMethods.MagicFile(_ptr, inName);
            return Marshal.PtrToStringAnsi(strPtr);
        }

        public string CheckBuffer(byte[] buffer)
        {
            UIntPtr nb = new UIntPtr((uint)buffer.Length);
            IntPtr strPtr = NativeMethods.MagicBuffer(_ptr, buffer, nb);
            return Marshal.PtrToStringAnsi(strPtr);
        }
        #endregion

        #region Error Messages

        public string GetLastErrorMessage()
        {
            IntPtr strPtr = NativeMethods.MagicError(_ptr);
            return Marshal.PtrToStringAnsi(strPtr);
        }
        #endregion

        #region Manage Flags
        public MagicFlags GetFlags()
        {
            return NativeMethods.MagicGetFlags(_ptr);
        }

        public void SetFlags(MagicFlags flags)
        {
            int ret = NativeMethods.MagicSetFlags(_ptr, flags);
            CheckError(ret);
        }
        #endregion

        #region (Static) Version
        public static int VersionInt()
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            return NativeMethods.MagicVersion();
        }
        public static Version VersionInstance()
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            int verInt = NativeMethods.MagicVersion();
            return new Version(verInt / 100, verInt % 100);
        }
        #endregion

        #region Load MagicFile
        /// <summary>
        /// Load a magic file.
        /// </summary>
        /// <param name="magicFile"></param>
        public void Load(string magicFile)
        {
            int ret = NativeMethods.MagicLoad(_ptr, magicFile);
            CheckError(ret);
        }
        #endregion

        #region Exception Utility
        public void CheckError(int ret)
        {
            if (ret < 0)
                throw new InvalidOperationException(GetLastErrorMessage());
        }
        #endregion
    }
}
