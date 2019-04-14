using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Joveler.FileMagician
{
    #region NativeMethods
    internal static class NativeMethods
    {
        #region Const
        public const string MsgInitFirstError = "Please call Magic.GlobalInit() first!";
        public const string MsgAlreadyInited = "Joveler.LibMagic is already initialized.";
        #endregion

        #region Fields
        internal static IntPtr hModule;
        public static bool Loaded => hModule != IntPtr.Zero;
        #endregion

        #region Windows kernel32 API
        internal static class Win32
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32.dll")]
            internal static extern int FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int SetDllDirectory([MarshalAs(UnmanagedType.LPWStr)] string lpPathName);
        }
        #endregion

        #region Linux libdl API
#pragma warning disable IDE1006 // 명명 스타일
        internal static class Linux
        {
            internal const int RTLD_NOW = 0x0002;
            internal const int RTLD_GLOBAL = 0x0100;

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr dlopen(string fileName, int flags);

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int dlclose(IntPtr handle);

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern string dlerror();

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr dlsym(IntPtr handle, string symbol);
        }
#pragma warning restore IDE1006 // 명명 스타일
        #endregion

        #region GetFuncPtr
        private static T GetFuncPtr<T>(string funcSymbol) where T : Delegate
        {
            IntPtr funcPtr;
#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                funcPtr = Win32.GetProcAddress(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}]", new Win32Exception());
            }
#if !NET451
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                funcPtr = Linux.dlsym(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}], {Linux.dlerror()}");
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
#endif

            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        internal static void LoadFunctions()
        {
            #region Open and Close
            MagicOpen = GetFuncPtr<magic_open>(nameof(magic_open));
            MagicClose = GetFuncPtr<magic_close>(nameof(magic_close));
            #endregion

            #region Operations
            MagicGetPath = GetFuncPtr<magic_getpath>(nameof(magic_getpath));
            MagicFile = GetFuncPtr<magic_file>(nameof(magic_file));
            MagicBuffer = GetFuncPtr<magic_buffer>(nameof(magic_buffer));

            MagicError = GetFuncPtr<magic_error>(nameof(magic_error));
            MagicGetFlags = GetFuncPtr<magic_getflags>(nameof(magic_getflags));
            MagicSetFlags = GetFuncPtr<magic_setflags>(nameof(magic_setflags));

            MagicVersion = GetFuncPtr<magic_version>(nameof(magic_version));
            MagicLoad = GetFuncPtr<magic_load>(nameof(magic_load));
            MagicLoadBuffers = GetFuncPtr<magic_load_buffers>(nameof(magic_load_buffers));

            MagicCompile = GetFuncPtr<magic_compile>(nameof(magic_compile));
            MagicCheck = GetFuncPtr<magic_check>(nameof(magic_check));
            MagicList = GetFuncPtr<magic_list>(nameof(magic_list));
            MagicErrno = GetFuncPtr<magic_errno>(nameof(magic_errno));

            MagicSetParam = GetFuncPtr<magic_setparam>(nameof(magic_setparam));
            MagicGetParam = GetFuncPtr<magic_getparam>(nameof(magic_getparam));
            #endregion
        }

        internal static void ResetFunctions()
        {
            #region Open and Close
            MagicOpen = null;
            MagicClose = null;
            #endregion

            #region Operations
            MagicGetPath = null;
            MagicFile = null;
            MagicBuffer = null;

            MagicError = null;
            MagicGetFlags = null;
            MagicSetFlags = null;

            MagicVersion = null;
            MagicLoad = null;
            MagicLoadBuffers = null;

            MagicCompile = null;
            MagicCheck = null;
            MagicList = null;
            MagicErrno = null;

            MagicSetParam = null;
            MagicGetParam = null;
            #endregion
        }
        #endregion

        #region libmagic Function Pointer
        #region Open and Close
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr magic_open(MagicFlags flags);
        internal static magic_open MagicOpen;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void magic_close(IntPtr ptr);
        internal static magic_close MagicClose;
        #endregion

        #region Operations
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr magic_getpath(IntPtr magicfile, int action);
        internal static magic_getpath MagicGetPath;

        /// <summary>
        /// Find type of named file.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr magic_file(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string inname);
        internal static magic_file MagicFile;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr magic_buffer(IntPtr ms, byte[] buf, UIntPtr nb);
        internal static magic_buffer MagicBuffer;
        

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr magic_error(IntPtr ms);
        internal static magic_error MagicError;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate MagicFlags magic_getflags(IntPtr ms);
        internal static magic_getflags MagicGetFlags;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_setflags(IntPtr ms, MagicFlags flags);
        internal static magic_setflags MagicSetFlags;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_version();
        internal static magic_version MagicVersion;

        /// <summary>
        /// Load a magic file.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_load(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string magicfile);
        internal static magic_load MagicLoad;

        /// <summary>
        /// Install a set of compiled magic buffers.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_load_buffers(
            IntPtr ms,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] buf,
            ref UIntPtr sizes, // size_t
            UIntPtr nbufs); // size_t
        internal static magic_load_buffers MagicLoadBuffers;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_compile(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string magicfile);
        internal static magic_compile MagicCompile;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_check(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string magicfile);
        internal static magic_check MagicCheck;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_list(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string magicfile);
        internal static magic_list MagicList;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_errno(IntPtr ms);
        internal static magic_errno MagicErrno;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_setparam(IntPtr ms, MagicParam param, ref UIntPtr val);
        internal static magic_setparam MagicSetParam;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_getparam(IntPtr ms, MagicParam param, ref UIntPtr val);
        internal static magic_getparam MagicGetParam;
        #endregion
        #endregion
    }
    #endregion
}
