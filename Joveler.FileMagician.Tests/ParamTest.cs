/*
    Copyright (C) 2019-2020 Hajin Jang

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

namespace Joveler.FileMagician.Tests
{
    public class ParamSnapshot
    {
        /// <summary>
        /// libmagic: 15, file: 50
        /// </summary>
        public ulong IndirMax { get; set; }
        /// <summary>
        /// libmagic: 30, file: 50
        /// </summary>
        public ulong NameMax { get; set; }
        /// <summary>
        /// libmagic: 128, file: 2KB
        /// </summary>
        public ulong ElfPhNumMax { get; set; } = 0;
        /// <summary>
        /// libmagic: 32KB, file: 32KB
        /// </summary>
        public ulong ElfShNumMax { get; set; } = 0;
        /// <summary>
        /// libmagic: 256, file: 256
        /// </summary>
        public ulong ElfNotesMax { get; set; } = 0;
        /// <summary>
        /// libmagic: 8KB, file: 8KB
        /// </summary>
        public ulong RegexMax { get; set; } = 0;
        /// <summary>
        /// libmagic: 1MB, file: 1MB
        /// </summary>
        public ulong BytesMax { get; set; } = 0;
        /// <summary>
        /// libmagic: ?, file: 65KB
        /// </summary>
        public ulong EncodingMax { get; set; } = 0;
        /// <summary>
        /// libmagic: ?, file: 128MB
        /// </summary>
        public ulong ElfShSizeMax { get; set; } = 0;

        public void PrintParams()
        {
            Console.WriteLine($"{nameof(MagicParam.IndirMax)}     : {IndirMax}");
            Console.WriteLine($"{nameof(MagicParam.NameMax)}      : {NameMax}");
            Console.WriteLine($"{nameof(MagicParam.ElfPhNumMax)}  : {ElfPhNumMax}");
            Console.WriteLine($"{nameof(MagicParam.ElfShNumMax)}  : {ElfShNumMax}");
            Console.WriteLine($"{nameof(MagicParam.ElfNotesMax)}  : {ElfNotesMax}");
            Console.WriteLine($"{nameof(MagicParam.RegexMax)}     : {RegexMax}");
            Console.WriteLine($"{nameof(MagicParam.BytesMax)}     : {BytesMax}");
            Console.WriteLine($"{nameof(MagicParam.EncodingMax)}  : {EncodingMax}"); 
            Console.WriteLine($"{nameof(MagicParam.ElfShSizeMax)} : {ElfShSizeMax}");
            Console.WriteLine();
        }

        public static ParamSnapshot CaptureSnapshot(Magic magic)
        {
            ParamSnapshot snapshot = new ParamSnapshot
            {
                IndirMax = magic.GetParam(MagicParam.IndirMax),
                NameMax = magic.GetParam(MagicParam.NameMax),
                ElfPhNumMax = magic.GetParam(MagicParam.ElfPhNumMax),
                ElfShNumMax = magic.GetParam(MagicParam.ElfShNumMax),
                ElfNotesMax = magic.GetParam(MagicParam.ElfNotesMax),
                RegexMax = magic.GetParam(MagicParam.RegexMax),
                BytesMax = magic.GetParam(MagicParam.BytesMax),
                EncodingMax = magic.GetParam(MagicParam.EncodingMax),
                ElfShSizeMax = magic.GetParam(MagicParam.ElfShSizeMax)
            };
            return snapshot;
        }

        public void RestoreSnapshot(Magic magic)
        {
            magic.SetParam(MagicParam.IndirMax, IndirMax);
            magic.SetParam(MagicParam.NameMax, NameMax);
            magic.SetParam(MagicParam.ElfPhNumMax, ElfPhNumMax);
            magic.SetParam(MagicParam.ElfShNumMax, ElfShNumMax);
            magic.SetParam(MagicParam.ElfNotesMax, ElfNotesMax);
            magic.SetParam(MagicParam.RegexMax, RegexMax);
            magic.SetParam(MagicParam.BytesMax, BytesMax);
            magic.SetParam(MagicParam.EncodingMax, EncodingMax);
            magic.SetParam(MagicParam.ElfShSizeMax, ElfShSizeMax);
        }
    }

    [TestClass]
    public class ParamTest
    {
        [TestMethod]
        public void ParamTests()
        {
            using (Magic magic = Magic.Open(TestSetup.MagicCompiledFile))
            {
                Console.WriteLine("[Default Values]");
                ParamSnapshot backup = ParamSnapshot.CaptureSnapshot(magic);
                backup.PrintParams();

                Console.WriteLine("[Set & Check New Values]");
                magic.SetParam(MagicParam.IndirMax, ushort.MaxValue);
                magic.SetParam(MagicParam.NameMax, ushort.MaxValue);
                magic.SetParam(MagicParam.ElfPhNumMax, ushort.MaxValue);
                magic.SetParam(MagicParam.ElfShNumMax, ushort.MaxValue);
                magic.SetParam(MagicParam.ElfNotesMax, ushort.MaxValue);
                magic.SetParam(MagicParam.RegexMax, ushort.MaxValue);
                magic.SetParam(MagicParam.BytesMax, 1024 * 1024);
                magic.SetParam(MagicParam.EncodingMax, 2 * ushort.MaxValue);
                magic.SetParam(MagicParam.ElfShSizeMax, 64 * 1024 * 1024);
                ParamSnapshot modified = ParamSnapshot.CaptureSnapshot(magic);
                modified.PrintParams();

                Assert.AreEqual((ulong)ushort.MaxValue, modified.IndirMax);
                Assert.AreEqual((ulong)ushort.MaxValue, modified.NameMax);
                Assert.AreEqual((ulong)ushort.MaxValue, modified.ElfPhNumMax);
                Assert.AreEqual((ulong)ushort.MaxValue, modified.ElfShNumMax);
                Assert.AreEqual((ulong)ushort.MaxValue, modified.ElfNotesMax);
                Assert.AreEqual((ulong)ushort.MaxValue, modified.RegexMax);
                Assert.AreEqual((ulong)(1024 * 1024), modified.BytesMax);
                Assert.AreEqual((ulong)(2 * ushort.MaxValue), modified.EncodingMax);
                Assert.AreEqual((ulong)(64 * 1024 * 1024), modified.ElfShSizeMax);

                // Restore backup
                backup.RestoreSnapshot(magic);
            }
        }
    }
}
