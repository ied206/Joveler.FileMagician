using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable RedundantExplicitArraySize
// ReSharper disable UnusedMember.Global

namespace Joveler.FileMagician
{
    public class Magic : IDisposable
    {
        #region Const
        #endregion

        #region Fields
        /// <summary>
        /// For magic_t
        /// </summary>
        private IntPtr _magicPtr;
        /// <summary>
        /// For LoadBuffer
        /// </summary>
        private readonly List<IntPtr> _magicBuffers;
        #endregion

        #region Constructor (private)
        private Magic(IntPtr ptr)
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            _magicPtr = ptr;
            _magicBuffers = new List<IntPtr>();
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
            if (_magicPtr == IntPtr.Zero)
                return;

            // Close magic_t
            NativeMethods.MagicClose(_magicPtr);
            _magicPtr = IntPtr.Zero;

            // Free database buffer if has been allocated
            foreach (IntPtr magicBufferPtr in _magicBuffers)
            {
                if (magicBufferPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(magicBufferPtr);
            }
            _magicBuffers.Clear();
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
                if (libPath == null)
                    throw new ArgumentNullException(nameof(libPath));

                libPath = Path.GetFullPath(libPath);
                if (!File.Exists(libPath))
                    throw new ArgumentException("Specified .dll file does not exist");

                // Set proper directory to search, unless LoadLibrary can fail when loading chained dll files.
                string libDir = Path.GetDirectoryName(libPath);
                if (libDir != null && !libDir.Equals(AppDomain.CurrentDomain.BaseDirectory))
                    NativeMethods.Win32.SetDllDirectory(libDir);

                NativeMethods.hModule = NativeMethods.Win32.LoadLibrary(libPath);

                // Reset dll search directory to prevent dll hijacking
                NativeMethods.Win32.SetDllDirectory(null);

                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());

                // Check if dll is valid (libmagic-1.dll)
                if (NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, nameof(NativeMethods.magic_open)) == IntPtr.Zero)
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

                libPath = Path.GetFullPath(libPath);
                if (!File.Exists(libPath))
                    throw new ArgumentException("Specified .so file does not exist");

                NativeMethods.hModule = NativeMethods.Linux.dlopen(libPath, NativeMethods.Linux.RTLD_NOW | NativeMethods.Linux.RTLD_GLOBAL);
                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}], {NativeMethods.Linux.dlerror()}");

                // Check if dll is valid libmagic.so
                if (NativeMethods.Linux.dlsym(NativeMethods.hModule, nameof(NativeMethods.magic_open)) == IntPtr.Zero)
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
        public static Magic Open() => Open(MagicFlags.NONE);

        public static Magic Open(MagicFlags flags)
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            IntPtr ptr = NativeMethods.MagicOpen(flags);
            if (ptr == null)
                throw new InvalidOperationException("Can't create magic");

            return new Magic(ptr);
        }

        public static Magic Open(string magicFile) => Open(magicFile, MagicFlags.NONE);

        public static Magic Open(string magicFile, MagicFlags flags)
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

        #region Load MagicFile
        /// <summary>
        /// Load a magic file.
        /// </summary>
        public void Load(string magicFile)
        {
            // Windows version of libmagic cannot handle unicode filepath.
            // If path includes an unicode char which cannot be converted to system ANSI locale, MagicLoad would fail.
            // In that case, fall back to buffer-based function.
            if (Win32Encoding.IsActiveCodePageCompatible(magicFile))
            { // In non-Windows platform, this path is always active.
                int ret = NativeMethods.MagicLoad(_magicPtr, magicFile);
                CheckError(ret);
            }
            else
            {
                byte[] magicBuffer;
                using (FileStream fs = new FileStream(magicFile, FileMode.Open, FileAccess.Read))
                {
                    magicBuffer = new byte[fs.Length];
                    fs.Read(magicBuffer, 0, magicBuffer.Length);
                }
                LoadBuffer(magicBuffer, 0, magicBuffer.Length);
            }
        }

        /// <summary>
        /// Install a set of compiled magic buffers.
        /// </summary>
        public void LoadBuffer(byte[] magicBuffer, int offset, int count)
        {
            IntPtr magicBufPtr = Marshal.AllocHGlobal(count);
            Marshal.Copy(magicBuffer, offset, magicBufPtr, count);
            _magicBuffers.Add(magicBufPtr);

            UIntPtr nbufs = (UIntPtr)1;
            UIntPtr[] sizes = new UIntPtr[1] { (UIntPtr)magicBuffer.Length };
            IntPtr[] buffers = new IntPtr[1] { magicBufPtr };

            int ret = NativeMethods.MagicLoadBuffers(_magicPtr, buffers, sizes, nbufs);
            CheckError(ret);
        }

        /// <summary>
        /// Install a set of compiled magic buffers.
        /// </summary>
        public unsafe void LoadBuffer(ReadOnlySpan<byte> magicSpan)
        {
            IntPtr magicBufPtr = Marshal.AllocHGlobal(magicSpan.Length);
            byte* butPtr = (byte*)magicBufPtr.ToPointer();
            for (int i = 0; i < magicSpan.Length; i++)
                butPtr[i] = magicSpan[i];

            UIntPtr nbufs = (UIntPtr)1;
            UIntPtr[] sizes = new UIntPtr[1] { (UIntPtr)magicSpan.Length };
            IntPtr[] buffers = new IntPtr[1] { magicBufPtr };

            int ret = NativeMethods.MagicLoadBuffers(_magicPtr, buffers, sizes, nbufs);
            CheckError(ret);
        }
        #endregion

        #region Check Type
        public string CheckFile(string inName)
        {
            // Windows version of libmagic cannot handle unicode filepath.
            // If path includes an unicode char which cannot be converted to system ANSI locale, MagicLoad would fail.
            // In that case, fall back to buffer-based function.
            if (Win32Encoding.IsActiveCodePageCompatible(inName))
            { // In non-Windows platform, this path is always active.
                IntPtr strPtr = NativeMethods.MagicFile(_magicPtr, inName);
                return Marshal.PtrToStringAnsi(strPtr);
            }
            else
            {
                int bytesRead;
                byte[] magicBuffer = new byte[256 * 1024]; // `file` command use 256KB buffer by default
                using (FileStream fs = new FileStream(inName, FileMode.Open, FileAccess.Read))
                {
                    bytesRead = fs.Read(magicBuffer, 0, magicBuffer.Length);
                }

                return CheckBuffer(magicBuffer, 0, bytesRead);
            }
        }

        public string CheckBuffer(byte[] buffer, int offset, int count)
        {
            ReadOnlySpan<byte> span = buffer.AsSpan(offset, count);
            return CheckBuffer(span);
        }

        public unsafe string CheckBuffer(ReadOnlySpan<byte> span)
        {
            IntPtr strPtr;
            fixed (byte* bufPtr = span)
            {
                strPtr = NativeMethods.MagicBuffer(_magicPtr, bufPtr, (UIntPtr)span.Length);
            }
            return Marshal.PtrToStringAnsi(strPtr);
        }
        #endregion

        #region Error Messages

        public string GetLastErrorMessage()
        {
            IntPtr strPtr = NativeMethods.MagicError(_magicPtr);
            return Marshal.PtrToStringAnsi(strPtr);
        }
        #endregion

        #region Manage Flags
        public MagicFlags GetFlags()
        {
            return NativeMethods.MagicGetFlags(_magicPtr);
        }

        public void SetFlags(MagicFlags flags)
        {
            int ret = NativeMethods.MagicSetFlags(_magicPtr, flags);
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

        #region Exception Utility
        public void CheckError(int ret)
        {
            if (ret < 0)
                throw new InvalidOperationException(GetLastErrorMessage());
        }
        #endregion
    }
}
