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
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly List<Tuple<IntPtr, UIntPtr>> _magicBuffers; // Ptr, Size
        #endregion

        #region Constructor (private)
        private Magic(IntPtr ptr)
        {
            Manager.EnsureLoaded();

            _magicPtr = ptr;
            _magicBuffers = new List<Tuple<IntPtr, UIntPtr>>();
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
            FreeMagicBuffers();
        }
        #endregion

        #region (Static) Open
        public static Magic Open() => Open(MagicFlags.None);

        public static Magic Open(MagicFlags flags)
        {
            Manager.EnsureLoaded();

            IntPtr ptr = Lib.MagicOpen(flags);
            if (ptr == null)
                throw new InvalidOperationException("Cannot create magic");

            return new Magic(ptr);
        }

        public static Magic Open(string magicFile) => Open(magicFile, MagicFlags.None);

        public static Magic Open(string magicFile, MagicFlags flags)
        {
            Manager.EnsureLoaded();

            IntPtr ptr = Lib.MagicOpen(flags);
            if (ptr == null)
                throw new InvalidOperationException("Cannot create magic");

            Magic magic = new Magic(ptr);
            magic.LoadMagicFile(magicFile);
            return magic;
        }
        #endregion
        
        #region (Static) Magic File Path
        /// <summary>
        /// Get default path of magicFile.
        /// NOTE: This function does not support Unicode on Windows.
        /// </summary>
        public static string GetDefaultMagicFilePath()
        {
            IntPtr strPtr = Lib.MagicGetPath(IntPtr.Zero, 0);
            return Marshal.PtrToStringAnsi(strPtr);
        }
        #endregion

        #region Load Magic Database
        /// <summary>
        /// Load a magic file.
        /// </summary>
        public void LoadMagicFile(string magicFile)
        {
            void InternalLoadMagicFile()
            {
                int ret = Lib.MagicLoad(_magicPtr, magicFile);
                CheckMagicError(ret);
            }

            if (magicFile == null)
            { // Load default magic database
                InternalLoadMagicFile();
                return;
            }

            // magic_load_buffers() : Does not support auto compiling
            // magic_load() : Does not support Unicode on Windows, support auto compiling
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !Helper.IsActiveCodePageCompatible(magicFile))
            {
                // magicFile is unicode-only path
                byte[] magicBuffer;
                using (FileStream fs = new FileStream(magicFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    magicBuffer = new byte[fs.Length];
                    fs.Read(magicBuffer, 0, magicBuffer.Length);
                }

                // Check if magicBuffer has NULL byte.
                if (magicBuffer.Any(x => x == 0))
                { // has NULL byte -> compiled .mgc
                    LoadMagicBuffer(magicBuffer, 0, magicBuffer.Length);
                }
                else
                { // Does not has NULL byte -> text.
                    // Copy to temp dir and load/compile from file
                    string tempFile = Path.GetTempFileName();
                    try
                    {
                        File.Copy(magicFile, tempFile, true);

                        InternalLoadMagicFile();
                    }
                    finally
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                }
                return;
            }

            InternalLoadMagicFile();
        }

        /// <summary>
        /// Install a compiled magic buffer.
        /// </summary>
        public void LoadMagicBuffer(byte[] magicBuf, int offset, int count)
        {
            CheckReadWriteArgs(magicBuf, offset, count);
            LoadMagicBuffer(magicBuf.AsSpan(offset, count));
        }

        /// <summary>
        /// Install a compiled magic buffer.
        /// </summary>
        public unsafe void LoadMagicBuffer(ReadOnlySpan<byte> magicSpan)
        {
            // Free and re-alloc magic buffers.
            FreeMagicBuffers();
            AllocMagicBuffer(magicSpan);

            // Call magic_load_buffers()
            GetMagicBufferPtrs(out IntPtr[] buffers, out UIntPtr[] sizes, out UIntPtr nbufs);
            int ret = Lib.MagicLoadBuffers(_magicPtr, buffers, sizes, nbufs);
            CheckMagicError(ret);
        }

        /// <summary>
        /// Install a set of compiled magic buffers.
        /// </summary>
        public void LoadMagicBuffers(IEnumerable<byte[]> magicBufs)
        {
            // Free and re-alloc magic buffers.
            FreeMagicBuffers();
            foreach (byte[] magicBuf in magicBufs)
                AllocMagicBuffer(magicBuf);

            // Call magic_load_buffers()
            GetMagicBufferPtrs(out IntPtr[] buffers, out UIntPtr[] sizes, out UIntPtr nbufs);
            int ret = Lib.MagicLoadBuffers(_magicPtr, buffers, sizes, nbufs);
            CheckMagicError(ret);
        }

        /// <summary>
        /// Install a set of compiled magic buffers.
        /// </summary>
        public void LoadMagicBuffers(IEnumerable<ArraySegment<byte>> magicBufs)
        {
            // Free and re-alloc magic buffers.
            FreeMagicBuffers();
            foreach (ArraySegment<byte> magicBufSeg in magicBufs)
                AllocMagicBuffer(magicBufSeg);

            // Call magic_load_buffers()
            GetMagicBufferPtrs(out IntPtr[] buffers, out UIntPtr[] sizes, out UIntPtr nbufs);
            int ret = Lib.MagicLoadBuffers(_magicPtr, buffers, sizes, nbufs);
            CheckMagicError(ret);
        }
        #endregion

        #region (private) Manage Magic Buffers
        /// <summary>
        /// Allocate and copy the magic database buffer into native heap.
        /// </summary>
        private unsafe void AllocMagicBuffer(ReadOnlySpan<byte> magicSpan)
        {
            // Allocate native heap
            IntPtr magicBufPtr = Marshal.AllocHGlobal(magicSpan.Length);

            // Copy the buffer to native heap from managed heap
            byte* butPtr = (byte*)magicBufPtr.ToPointer();
            for (int i = 0; i < magicSpan.Length; i++)
                butPtr[i] = magicSpan[i];

            // Add the new heap into the heap list
            _magicBuffers.Add(new Tuple<IntPtr, UIntPtr>(magicBufPtr, (UIntPtr)magicSpan.Length));
        }

        private void GetMagicBufferPtrs(out IntPtr[] buffers, out UIntPtr[] sizes, out UIntPtr nbufs)
        {
            buffers = new IntPtr[_magicBuffers.Count];
            sizes = new UIntPtr[_magicBuffers.Count];
            nbufs = (UIntPtr)_magicBuffers.Count;

            for (int i = 0; i < _magicBuffers.Count; i++)
            {
                buffers[i] = _magicBuffers[i].Item1;
                sizes[i] = _magicBuffers[i].Item2;
            }
        }

        /// <summary>
        /// Ckear _magicBuffers, which contains the magic database.
        /// </summary>
        /// <remarks>
        /// magic_load_buffers() requires the buffer caller provided must be alive until magic handle is freed.
        /// </remarks>
        private void FreeMagicBuffers()
        {
            // Free database buffer if has been allocated
            for (int i = 0; i < _magicBuffers.Count; i++)
            {
                IntPtr magicBufPtr = _magicBuffers[i].Item1;
                if (magicBufPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(magicBufPtr);
            }
            _magicBuffers.Clear();
        }
        #endregion

        #region Check Type
        /// <summary>
        /// Check type of a given file by inspecting first 256KB.
        /// </summary>
        /// <param name="inName">File to check its type.</param>
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

        /// <summary>
        /// Check type of a given file.
        /// </summary>
        /// <param name="inName">File to check its type.</param>
        /// <param name="checkSize">How many bytes to check?</param>
        /// <returns></returns>
        public string CheckFile(string inName, int checkSize)
        {
            int bytesRead;
            byte[] magicBuffer = new byte[checkSize];
            using (FileStream fs = new FileStream(inName, FileMode.Open, FileAccess.Read))
            {
                bytesRead = fs.Read(magicBuffer, 0, magicBuffer.Length);
            }

            return CheckBuffer(magicBuffer, 0, bytesRead);
        }

        /// <summary>
        /// Check type of a given buffer.
        /// </summary>
        public string CheckBuffer(byte[] buffer, int offset, int count)
        {
            CheckReadWriteArgs(buffer, offset, count);

            ReadOnlySpan<byte> span = buffer.AsSpan(offset, count);
            return CheckBuffer(span);
        }

        /// <summary>
        /// Check type of a given buffer.
        /// </summary>
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

        #region Manage Flags
        public MagicFlags GetFlags()
        {
            return Lib.MagicGetFlags(_magicPtr);
        }

        public void SetFlags(MagicFlags flags)
        {
            int ret = Lib.MagicSetFlags(_magicPtr, flags);
            CheckMagicError(ret);
        }
        #endregion

        #region Manage Params
        /// <summary>
        /// Get various limits related to the magic library.
        /// </summary>
        public unsafe ulong GetParam(MagicParam param)
        {
            UIntPtr size = new UIntPtr(0); // size_t
            int ret = Lib.MagicGetParam(_magicPtr, param, &size);
            CheckMagicError(ret);
            return size.ToUInt64();
        }

        /// <summary>
        /// Set various limits related to the magic library.
        /// </summary>
        public unsafe void SetParam(MagicParam param, ulong value)
        {
            UIntPtr size = new UIntPtr(value); // size_t
            int ret = Lib.MagicSetParam(_magicPtr, param, &size);
            CheckMagicError(ret);
        }
        #endregion

        #region Compile
        /// <summary>
        /// Compile magic database file passed in <paramref name="magicSrcFile"/> to <paramref name="magicDestFile"/>.
        /// </summary>
        /// <remarks>
        /// WARNING: Current Directory is altered and rollbacked in the process, it may cause issues on multi-threaded application.
        /// Use the other override in order to avoid this problem.
        /// </remarks>
        public void Compile(string magicSrcFile, string magicDestFile)
        {
            // magic_compile() creates magic.mgc file on current directory.
            // To control dest file name and location, operate on temp directory.
            string curDirBak = Environment.CurrentDirectory;
            string tempDir = Helper.GetTempDir();
            try
            {
                string srcFile = Path.Combine(tempDir, "magic.src");
                string destFile = Path.Combine(tempDir, "magic.mgc");
                File.Copy(magicSrcFile, srcFile, true);

                Environment.CurrentDirectory = tempDir;
                int ret = Lib.MagicCompile(_magicPtr, srcFile);
                CheckMagicError(ret);

                File.Copy(destFile, magicDestFile, true);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                Environment.CurrentDirectory = curDirBak;
            }
        }

        /// <summary>
        /// Compile magic database file passed in <paramref name="magicSrcFile"/> to [CurrentDirectory/magic.mgc].
        /// </summary>
        /// <remarks>
        /// The destination path is hard-wired by libmagic itself. 
        /// This method does not change current directory.
        /// </remarks>
        /// <returns>
        /// Returns created mgc file path
        /// </returns>
        public string Compile(string magicSrcFile)
        {
            // magic_compile() creates magic.mgc file on current directory.
            // To control dest file name and location, operate on temp directory.
            int ret = Lib.MagicCompile(_magicPtr, magicSrcFile);
            CheckMagicError(ret);

            string destFile = Path.Combine(Environment.CurrentDirectory, "magic.mgc");
            return destFile;
        }
        #endregion

        #region (static, property) Version
        public static int VersionInt
        {
            get
            {
                Manager.EnsureLoaded();

                return Lib.MagicVersion();
            }
        }

        public static Version Version
        {
            get
            {
                Manager.EnsureLoaded();

                int verInt = Lib.MagicVersion();
                return new Version(verInt / 100, verInt % 100);
            }
        }
        #endregion

        #region (private) Exception Utility
        private void CheckMagicError(int ret)
        {
            if (ret < 0)
                throw new InvalidOperationException(GetLastErrorMessage());
        }

        private string GetLastErrorMessage()
        {
            IntPtr strPtr = Lib.MagicError(_magicPtr);
            return Marshal.PtrToStringAnsi(strPtr);
        }
        #endregion

        #region (private) Check Arguments
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckReadWriteArgs(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentOutOfRangeException(nameof(count));
        }
        #endregion
    }
}
