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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable RedundantExplicitArraySize
// ReSharper disable UnusedMember.Global

namespace Joveler.FileMagician
{
    public class Magic : IDisposable
    {
        #region LoadManager
        internal static MagicLoadManager Manager = new MagicLoadManager();
        internal static MagicLoader Lib => Manager.Lib;
        #endregion

        #region (static) GlobalInit, GlobalCleanup
        public static void GlobalInit() => Manager.GlobalInit();
        public static void GlobalInit(string libPath) => Manager.GlobalInit(libPath);
        public static void GlobalCleanup() => Manager.GlobalCleanup();
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
            Manager.EnsureLoaded();

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
            Lib.MagicClose(_magicPtr);
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

        #region (Static) Open
        public static Magic Open() => Open(MagicFlags.NONE);

        public static Magic Open(MagicFlags flags)
        {
            Manager.EnsureLoaded();

            IntPtr ptr = Lib.MagicOpen(flags);
            if (ptr == null)
                throw new InvalidOperationException("Can't create magic");

            return new Magic(ptr);
        }

        public static Magic Open(string magicFile) => Open(magicFile, MagicFlags.NONE);

        public static Magic Open(string magicFile, MagicFlags flags)
        {
            Manager.EnsureLoaded();

            IntPtr ptr = Lib.MagicOpen(flags);
            if (ptr == null)
                throw new InvalidOperationException("Can't create magic");

            Magic magic = new Magic(ptr);
            magic.Load(magicFile);
            return magic;
        }
        #endregion

        #region (Static) Magic File Path
        /// <summary>
        /// Get default (or given) path of magicFile, and autoload that file.
        /// </summary>
        public static string GetPath(string magicFile) => GetPath(magicFile, false);

        /// <summary>
        /// Get default (or given) path of magicFile.
        /// </summary>
        public static string GetPath(string magicFile, bool autoLoad)
        {
            Manager.EnsureLoaded();

            IntPtr magicFilePtr = Marshal.StringToHGlobalAnsi(magicFile);
            try
            {
                IntPtr strPtr = Lib.MagicGetPath(magicFilePtr, autoLoad ? 0 : -1);
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
            byte[] magicBuffer;
            using (FileStream fs = new FileStream(magicFile, FileMode.Open, FileAccess.Read))
            {
                magicBuffer = new byte[fs.Length];
                fs.Read(magicBuffer, 0, magicBuffer.Length);
            }
            LoadBuffer(magicBuffer, 0, magicBuffer.Length);
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

            int ret = Lib.MagicLoadBuffers(_magicPtr, buffers, sizes, nbufs);
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

            int ret = Lib.MagicLoadBuffers(_magicPtr, buffers, sizes, nbufs);
            CheckError(ret);
        }
        #endregion

        #region Check Type
        public string CheckFile(string inName)
        {
            int bytesRead;
            byte[] magicBuffer = new byte[256 * 1024]; // `file` command use 256KB buffer by default
            using (FileStream fs = new FileStream(inName, FileMode.Open, FileAccess.Read))
            {
                bytesRead = fs.Read(magicBuffer, 0, magicBuffer.Length);
            }

            return CheckBuffer(magicBuffer, 0, bytesRead);
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
                strPtr = Lib.MagicBuffer(_magicPtr, bufPtr, (UIntPtr)span.Length);
            }
            return Marshal.PtrToStringAnsi(strPtr);
        }
        #endregion

        #region Error Messages

        public string GetLastErrorMessage()
        {
            IntPtr strPtr = Lib.MagicError(_magicPtr);
            return Marshal.PtrToStringAnsi(strPtr);
        }
        #endregion

        #region Manage Flags
        public MagicFlags GetFlags()
        {
            return Lib.MagicGetFlags(_magicPtr);
        }

        public void SetFlags(MagicFlags flags)
        {
            int ret = Lib.MagicSetFlags(_magicPtr, flags);
            CheckError(ret);
        }
        #endregion

        #region (Static) Version
        public static int VersionInt()
        {
            Manager.EnsureLoaded();

            return Lib.MagicVersion();
        }
        public static Version VersionInstance()
        {
            Manager.EnsureLoaded();

            int verInt = Lib.MagicVersion();
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
