/*
    Copyright (C) 2023 Hajin Jang

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
using System.IO;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class CompileTest
    {
        private readonly object _randomLock = new object();
        private readonly Random _random = new Random();

        [TestMethod]
        public void CompileOneParam()
        {
            uint testId = 0;
            lock (_randomLock)
            {
                testId = (uint)_random.Next();
            }

            string tempDir = TestHelper.GetTempDir();
            string magicSrcPath = Path.Combine(tempDir, $"{testId:x8}");
            string magicMgcPath = Path.Combine(tempDir, $"{testId:x8}.mgc");
            try
            {
                File.Copy(TestSetup.MagicSourceFile, magicSrcPath);

                using (Magic magic = Magic.Open())
                {
                    magic.Compile(magicSrcPath);
                }

                Assert.IsTrue(File.Exists(magicMgcPath));
                Assert.IsTrue(Magic.IsFileCompiledMagic(magicMgcPath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            };
        }

        [TestMethod]
        public void CompileTwoParams()
        {
            uint testId = 0;
            lock (_randomLock)
            {
                testId = (uint)_random.Next();
            }

            string tempSrcDir = TestHelper.GetTempDir();
            string tempDestDir = TestHelper.GetTempDir();
            string magicSrcPath = Path.Combine(tempSrcDir, $"{testId:x8}");
            string magicMgcPath = Path.Combine(tempDestDir, $"{testId:x8}.mgc");
            try
            {
                File.Copy(TestSetup.MagicSourceFile, magicSrcPath);

                using (Magic magic = Magic.Open())
                {
                    magic.Compile(magicSrcPath, magicMgcPath);
                }

                Assert.IsTrue(File.Exists(magicMgcPath));
                Assert.IsTrue(Magic.IsFileCompiledMagic(magicMgcPath));
            }
            finally
            {
                if (Directory.Exists(tempSrcDir))
                    Directory.Delete(tempSrcDir, true);
                if (Directory.Exists(tempDestDir))
                    Directory.Delete(tempDestDir, true);
            };
        }
    }
}
