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

using Joveler.DynLoader;
using System;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Joveler.FileMagician
{
    #region NativeMethods
    internal class MagicLoader : DynLoaderBase
    {
        #region Constructor
        public MagicLoader() : base() { }
        public MagicLoader(string libPath) : base(libPath) { }
        #endregion

        #region (override) DefaultLibFileName
        protected override string DefaultLibFileName
        {
            get
            {
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "libmagic.so.1";
#endif
                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            #region Open and Close
            MagicOpen = GetFuncPtr<magic_open>(nameof(magic_open));
            MagicClose = GetFuncPtr<magic_close>(nameof(magic_close));
            #endregion

            #region Operations
            MagicGetPath = GetFuncPtr<magic_getpath>(nameof(magic_getpath));
            //MagicFile = GetFuncPtr<magic_file>(nameof(magic_file));
            MagicBuffer = GetFuncPtr<magic_buffer>(nameof(magic_buffer));

            MagicError = GetFuncPtr<magic_error>(nameof(magic_error));
            MagicGetFlags = GetFuncPtr<magic_getflags>(nameof(magic_getflags));
            MagicSetFlags = GetFuncPtr<magic_setflags>(nameof(magic_setflags));

            MagicVersion = GetFuncPtr<magic_version>(nameof(magic_version));
            //MagicLoad = GetFuncPtr<magic_load>(nameof(magic_load));
            MagicLoadBuffers = GetFuncPtr<magic_load_buffers>(nameof(magic_load_buffers));

            MagicCompile = GetFuncPtr<magic_compile>(nameof(magic_compile));
            MagicCheck = GetFuncPtr<magic_check>(nameof(magic_check));
            MagicList = GetFuncPtr<magic_list>(nameof(magic_list));
            MagicErrno = GetFuncPtr<magic_errno>(nameof(magic_errno));

            MagicSetParam = GetFuncPtr<magic_setparam>(nameof(magic_setparam));
            MagicGetParam = GetFuncPtr<magic_getparam>(nameof(magic_getparam));
            #endregion
        }

        protected override void ResetFunctions()
        {
            #region Open and Close
            MagicOpen = null;
            MagicClose = null;
            #endregion

            #region Operations
            MagicGetPath = null;
            //MagicFile = null;
            MagicBuffer = null;

            MagicError = null;
            MagicGetFlags = null;
            MagicSetFlags = null;

            MagicVersion = null;
            //MagicLoad = null;
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
        internal magic_open MagicOpen;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void magic_close(IntPtr ptr);
        internal magic_close MagicClose;
        #endregion

        #region Operations
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr magic_getpath(IntPtr magicfile, int action);
        internal magic_getpath MagicGetPath;

        /// <summary>
        /// Find type of named file.
        /// </summary>
        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //internal delegate IntPtr magic_file(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string inname);
        //internal magic_file MagicFile;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate IntPtr magic_buffer(IntPtr ms, byte* buf, UIntPtr nb);
        internal magic_buffer MagicBuffer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr magic_error(IntPtr ms);
        internal magic_error MagicError;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate MagicFlags magic_getflags(IntPtr ms);
        internal magic_getflags MagicGetFlags;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_setflags(IntPtr ms, MagicFlags flags);
        internal magic_setflags MagicSetFlags;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_version();
        internal magic_version MagicVersion;

        /// <summary>
        /// Load a magic file.
        /// </summary>
        // [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        // internal delegate int magic_load(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string magicfile);
        // internal magic_load MagicLoad;

        /// <summary>
        /// Install a set of compiled magic buffers.
        /// </summary>
        /// <remarks>
        /// int magic_load_buffers(magic_t, void **, size_t *, size_t);
        /// </remarks>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_load_buffers(
            IntPtr ms,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] buf, // void**
            [MarshalAs(UnmanagedType.LPArray)] UIntPtr[] sizes, // size_t*
            UIntPtr nbufs); // size_t
        internal magic_load_buffers MagicLoadBuffers;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_compile(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string magicfile);
        internal magic_compile MagicCompile;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_check(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string magicfile);
        internal magic_check MagicCheck;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_list(IntPtr ms, [MarshalAs(UnmanagedType.LPStr)] string magicfile);
        internal magic_list MagicList;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_errno(IntPtr ms);
        internal magic_errno MagicErrno;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_setparam(IntPtr ms, MagicParam param, ref UIntPtr val);
        internal magic_setparam MagicSetParam;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int magic_getparam(IntPtr ms, MagicParam param, ref UIntPtr val);
        internal magic_getparam MagicGetParam;
        #endregion
        #endregion
    }
    #endregion
}
