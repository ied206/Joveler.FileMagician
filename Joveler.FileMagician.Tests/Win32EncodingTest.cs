/*
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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class Win32EncodingTest
    {
        [TestMethod]
        public void IsActiveCodePageCompatible()
        {
            // Assumption : No known non-unicode encoding support Korean, non-ASCII latin, Chinese characters at once.
            const string compatStr = "123";
            const string incompatStr = "ᄒᆞᆫ글ḀḘ韓國";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            { // Call WideCharToMultiByte to test if a string is compatible with active code page
                Assert.IsTrue(Win32Encoding.IsActiveCodePageCompatible(compatStr));
                Assert.IsFalse(Win32Encoding.IsActiveCodePageCompatible(incompatStr));
            }
            else
            { // Always report true in non-Windows platforms
                Assert.IsTrue(Win32Encoding.IsActiveCodePageCompatible(compatStr));
                Assert.IsTrue(Win32Encoding.IsActiveCodePageCompatible(incompatStr));
            }
        }
    }
}
