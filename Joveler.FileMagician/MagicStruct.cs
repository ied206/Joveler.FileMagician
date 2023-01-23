/*
    C# pinvoke wrapper written by Hajin Jang
    Copyright (C) 2019-2023 Hajin Jang

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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

            // Close magic_t instance.
            Lib.MagicClose(_magicPtr);
            _magicPtr = IntPtr.Zero;

            // Free database buffer if it has been allocated.
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
            void InternalLoadMagicFile(string filepath)
            {
                int ret = Lib.MagicLoad(_magicPtr, filepath);
                CheckMagicError(ret);
            }

            // [*] Stage 01: magicFile is null, load default magic database
            if (magicFile == null)
            {
                InternalLoadMagicFile(null);
                return;
            }

            // [*] Stage 02: If a path is an Unicode path and the OS is Windows, use a workaround.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !Helper.IsActiveCodePageCompatible(magicFile))
            {
                // magicFile path requires Unicode (cannot be represented on active code page).
                byte[] magicBuffer;
                using (FileStream fs = new FileStream(magicFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    magicBuffer = new byte[fs.Length];
                    fs.Read(magicBuffer, 0, magicBuffer.Length);
                }

                // magic_load_buffers(): Does not support auto-compile.
                // magic_load() : Does not support Unicode on Windows, supports auto-compile. (See src/apprentice.c - apprentice_1())

                // Compiled magic database starts with 0xF11E041C. The database can be both LE or BE.
                if (IsBufferCompiledMagic(magicBuffer))
                { // Compiled magic database
                    LoadMagicBuffer(magicBuffer, 0, magicBuffer.Length);
                }
                else
                { // Copy to temp dir and load/compile from file
                    string tempFile = Path.GetTempFileName();
                    try
                    {
                        File.Copy(magicFile, tempFile, true);
                        InternalLoadMagicFile(tempFile);
                    }
                    finally
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                }
                return;
            }

            InternalLoadMagicFile(magicFile);
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
        /// <param name="magicSrcFile">Magic database as text to compile.</param>
        /// <param name="magicDestFile">Output path of a compiled magic database</param>
        /// <remarks>
        /// WARNING: Current Directory is altered and rollbacked in the process, it may cause issues on multi-threaded application.
        /// Use the other override in order to avoid this problem.
        /// </remarks>
        public void Compile(string magicSrcFile, string magicDestFile)
        {
            // magic_compile() creates magic.mgc file on same directory as source.
            // To control dest file name and location, operate on temp directory.
            string tempDir = Helper.GetTempDir();
            try
            {
                string tempSrcFile = Path.Combine(tempDir, "magic.src");
                string tempDestFile = Path.Combine(tempDir, "magic.src.mgc");
                File.Copy(magicSrcFile, tempSrcFile, true);

                int ret = Lib.MagicCompile(_magicPtr, tempSrcFile);
                CheckMagicError(ret);

                File.Copy(tempDestFile, magicDestFile, true);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Compile magic database file passed in <paramref name="magicSrcFile"/> to magic.mgc on same directory.
        /// Compiled file path would be $"{magicSrcFile}.mgc".
        /// </summary>
        /// <remarks>
        /// The destination path is hard-wired by libmagic itself. 
        /// This method does not change current directory.
        /// </returns>
        public void Compile(string magicSrcFile)
        {
            // magic_compile() creates magic.mgc file on same directory as source.
            // Filename would be $"{magicSrcFile}.mgc".
            int ret = Lib.MagicCompile(_magicPtr, magicSrcFile);
            CheckMagicError(ret);
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

        #region (static) Is{File,Buffer}CompiledMagic
        public static bool IsFileCompiledMagic(string magicFile)
        {
            int readBytes = 0;
            byte[] buffer = new byte[4];
            using (FileStream fs = new FileStream(magicFile, FileMode.Open, FileAccess.Read))
            {
                readBytes = fs.Read(buffer, 0, buffer.Length);
            }
            return IsBufferCompiledMagic(buffer, 0, readBytes);
        }

        public static bool IsBufferCompiledMagic(byte[] buffer, int offset, int count)
        {
            if (count < offset + 4)
                return false;

            uint mgcMagic = BitConverter.ToUInt32(buffer, offset);
            return IsNumberCompiledMagic(mgcMagic);
        }

        public static bool IsBufferCompiledMagic(Span<byte> span)
        {
            if (span.Length < 4)
                return false;

            uint mgcMagic = 0;
#if NETFRAMEWORK || NETSTANDARD
            byte[] buffer = new byte[4]
            {
                span[0],
                span[1],
                span[2],
                span[3],
            };
            mgcMagic = BitConverter.ToUInt32(buffer, 0);
#else
            mgcMagic = BitConverter.ToUInt32(span);
#endif
            return IsNumberCompiledMagic(mgcMagic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNumberCompiledMagic(uint magicNums)
        {
            return magicNums == 0xF11E041C || magicNums == 0x1C041EF1;
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
            if (strPtr == IntPtr.Zero)
                return "Unknown libmagic error.";
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
