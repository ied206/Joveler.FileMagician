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
    [TestClass]
    public class ParamTest
    {
        [TestMethod]
        public void Param()
        {
            ushort inDirMax;
            ushort nameMax;
            ushort elfPhNumMax;
            ushort elfShNumMax;
            ushort elfNotesMax;
            ushort regexMax;
            ulong bytesMax;

            void ReadParams(Magic magic)
            {
                inDirMax = (ushort)magic.GetParam(MagicParam.InDirMax);
                nameMax = (ushort)magic.GetParam(MagicParam.NameMax);
                elfPhNumMax = (ushort)magic.GetParam(MagicParam.ElfPhNumMax);
                elfShNumMax = (ushort)magic.GetParam(MagicParam.ElfShNumMax);
                elfNotesMax = (ushort)magic.GetParam(MagicParam.ElfNotesMax);
                regexMax = (ushort)magic.GetParam(MagicParam.RegexMax);
                bytesMax = magic.GetParam(MagicParam.BytesMax);
            }

            void PrintParams()
            {
                Console.WriteLine($"{nameof(MagicParam.InDirMax)}    : {inDirMax}");
                Console.WriteLine($"{nameof(MagicParam.NameMax)}     : {nameMax}");
                Console.WriteLine($"{nameof(MagicParam.ElfPhNumMax)} : {elfPhNumMax}");
                Console.WriteLine($"{nameof(MagicParam.ElfShNumMax)} : {elfShNumMax}");
                Console.WriteLine($"{nameof(MagicParam.ElfNotesMax)} : {elfNotesMax}");
                Console.WriteLine($"{nameof(MagicParam.RegexMax)}    : {regexMax}");
                Console.WriteLine($"{nameof(MagicParam.BytesMax)}    : {bytesMax}");
            }

            using (Magic magic = Magic.Open(TestSetup.MagicFile))
            {
                Console.WriteLine("[Default Values]");
                ReadParams(magic);
                PrintParams();

                Console.WriteLine("[Set New Values]");
                magic.SetParam(MagicParam.InDirMax, ushort.MaxValue);
                magic.SetParam(MagicParam.NameMax, ushort.MaxValue);
                magic.SetParam(MagicParam.ElfPhNumMax, ushort.MaxValue);
                magic.SetParam(MagicParam.ElfShNumMax, ushort.MaxValue);
                magic.SetParam(MagicParam.ElfNotesMax, ushort.MaxValue);
                magic.SetParam(MagicParam.RegexMax, ushort.MaxValue);
                magic.SetParam(MagicParam.BytesMax, 16 * ushort.MaxValue);
                ReadParams(magic);
                PrintParams();
                Assert.AreEqual((ulong)ushort.MaxValue, inDirMax);
                Assert.AreEqual((ulong)ushort.MaxValue, nameMax);
                Assert.AreEqual((ulong)ushort.MaxValue, elfPhNumMax);
                Assert.AreEqual((ulong)ushort.MaxValue, elfShNumMax);
                Assert.AreEqual((ulong)ushort.MaxValue, elfNotesMax);
                Assert.AreEqual((ulong)ushort.MaxValue, regexMax);
                Assert.AreEqual((ulong)(16 * ushort.MaxValue), bytesMax);
            }
        }
    }
}
