﻿/*
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class CheckTest
    {
        private class TypeInfo
        {
            public readonly string FileType;
            public readonly string MimeType;
            public readonly string MimeEncoding;
            public readonly string Extension;

            public TypeInfo(string fileType, string mimeType, string mimeEncoding, string extension = "???")
            {
                FileType = fileType;
                MimeType = mimeType;
                MimeEncoding = mimeEncoding;
                Extension = extension;
            }
        }

        private readonly Dictionary<string, TypeInfo> _fileTypeDict = new Dictionary<string, TypeInfo>(StringComparer.OrdinalIgnoreCase)
        {
            // Text File
            ["ANSI.txt"] = new TypeInfo("ASCII text, with no line terminators", "text/plain", "us-ascii"),
            ["EUC-KR.txt"] = new TypeInfo("ISO-8859 text, with no line terminators", "text/plain", "iso-8859-1"),
            ["UTF16BE_EN_wBOM.txt"] = new TypeInfo("Unicode text, UTF-16, big-endian text, with no line terminators", "text/plain", "utf-16be"),
            ["UTF16BE_KR_wBOM.txt"] = new TypeInfo("Unicode text, UTF-16, big-endian text, with no line terminators", "text/plain", "utf-16be"),
            ["UTF16LE_EN_wBOM.txt"] = new TypeInfo("Unicode text, UTF-16, little-endian text, with no line terminators", "text/plain", "utf-16le"),
            ["UTF16LE_KR_wBOM.txt"] = new TypeInfo("Unicode text, UTF-16, little-endian text, with no line terminators", "text/plain", "utf-16le"),
            ["UTF8_EN_wBOM.txt"] = new TypeInfo("Unicode text, UTF-8 (with BOM) text, with no line terminators", "text/plain", "utf-8"),
            ["UTF8_EN_woBOM.txt"] = new TypeInfo("ASCII text, with no line terminators", "text/plain", "us-ascii"),
            ["UTF8_KR_wBOM.txt"] = new TypeInfo("Unicode text, UTF-8 (with BOM) text, with no line terminators", "text/plain", "utf-8"),
            ["UTF8_KR_woBOM.txt"] = new TypeInfo("Unicode text, UTF-8 text, with no line terminators", "text/plain", "utf-8"),
            // Hancom Office NEO (2016) - .cell & .show is not exact : Hancom Office suite use compound file format similar to Microsoft Office 2003.
            ["Hancell2016.cell"] = new TypeInfo("Microsoft OOXML", "application/octet-stream", "binary"),
            ["Hanshow2016.show"] = new TypeInfo("Microsoft PowerPoint 2007+", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "binary"),
            ["HWP2016.hwp"] = new TypeInfo("Hangul (Korean) Word Processor File 5.x", "application/x-hwp", "binary", "hwp"),
            // LibreOffice 6.0.7.3
            ["LibreCalc6.ods"] = new TypeInfo("OpenDocument Spreadsheet", "application/vnd.oasis.opendocument.spreadsheet", "binary", "ods"),
            ["LibreImpress6.odp"] = new TypeInfo("OpenDocument Presentation", "application/vnd.oasis.opendocument.presentation", "binary", "odp"),
            ["LibreWriter6.odt"] = new TypeInfo("OpenDocument Text", "application/vnd.oasis.opendocument.text", "binary", "odt"),
            // Microsoft Office 2019
            ["Office2019.docx"] = new TypeInfo("Microsoft Word 2007+", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "binary"),
            ["Office2019.pptx"] = new TypeInfo("Microsoft PowerPoint 2007+", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "binary"),
            ["Office2019.xlsx"] = new TypeInfo("Microsoft Excel 2007+", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "binary"),
            // Archive Format
            ["Samples.7z"] = new TypeInfo("7-zip archive data, version 0.3", "application/x-7z-compressed", "binary", "7z/cb7"),
            ["Samples.tar"] = new TypeInfo("POSIX tar archive (GNU)", "application/x-tar", "binary", "tar/gtar"),
            ["Samples.tar.bz2"] = new TypeInfo("bzip2 compressed data, block size = 900k", "application/x-bzip2", "binary"),
            // Originally application/x-xz, but broken in 5.40, look https://bugs.astron.com/view.php?id=257.
            ["Samples.tar.xz"] = new TypeInfo("XZ compressed data, checksum CRC64", "application/octet-stream", "binary"),
            ["Samples.zip"] = new TypeInfo("Zip archive data, at least v2.0 to extract, compression method=deflate", "application/zip", "binary"),
            ["Samples.alz"] = new TypeInfo("ALZ archive data", "application/octet-stream", "binary", "alz"),
            ["Samples.egg"] = new TypeInfo("EGG archive data, version 1.0", "application/octet-stream", "binary", "egg"),
            ["Samples.rar"] = new TypeInfo("RAR archive data, v4, os: Win32", "application/x-rar", "binary", "rar/cbr"),
            ["Samples.rar5"] = new TypeInfo("RAR archive data, v5", "application/x-rar", "binary", "rar"),
            // Image Format
            ["Logo.bmp"] = new TypeInfo("PC bitmap, Windows 3.x format, 128 x 128 x 4, image size 8192, cbSize 8310, bits offset 118", "image/x-ms-bmp", "binary", "bmp"),
            ["Logo.bpg"] = new TypeInfo("BPG (Better Portable Graphics)", "image/bpg", "binary"),
            ["Logo.jpg"] = new TypeInfo("JPEG image data, JFIF standard 1.01, aspect ratio, density 1x1, segment length 16, baseline, precision 8, 128x128, components 3", "image/jpeg", "binary", "jpeg/jpg/jpe/jfif"),
            ["Logo.png"] = new TypeInfo("PNG image data, 128 x 128, 8-bit/color RGBA, non-interlaced", "image/png", "binary", "png"),
            ["Logo.svg"] = new TypeInfo("SVG Scalable Vector Graphics image", "image/svg+xml", "us-ascii"),
            ["Logo.webp"] = new TypeInfo("RIFF (little-endian) data, Web/P image", "image/webp", "binary", "webp"),
            // Database + Unicode-only path test 
            ["ᄒᆞᆫ글ḀḘ韓國.sqlite"] = new TypeInfo("SQLite 3.x database, last written using SQLite version 3027002", "application/x-sqlite3", "binary", "sqlite/sqlite3/db/dbe"),
        };

        [TestMethod]
        public void FileType()
        {
            // MagicBuffer
            foreach (var kv in _fileTypeDict)
            {
                string sampleFileName = kv.Key;
                TypeInfo ti = kv.Value;
                Template(sampleFileName, 0, MagicFlags.None, ti.FileType);
            }
        }

        [TestMethod]
        public void MimeType()
        {
            foreach (var kv in _fileTypeDict)
            {
                string sampleFileName = kv.Key;
                TypeInfo ti = kv.Value;
                Template(sampleFileName, 1, MagicFlags.MimeType, ti.MimeType);
            }
        }

        [TestMethod]
        public void MimeEncoding()
        {
            foreach (var kv in _fileTypeDict)
            {
                string sampleFileName = kv.Key;
                TypeInfo ti = kv.Value;
                Template(sampleFileName, 2, MagicFlags.MimeEncoding, ti.MimeEncoding);
            }
        }

        [TestMethod]
        public void Extension()
        {
            foreach (var kv in _fileTypeDict)
            {
                string sampleFileName = kv.Key;
                TypeInfo ti = kv.Value;
                Template(sampleFileName, 3, MagicFlags.Extension, ti.Extension);
            }
        }

        public static void Template(string sampleFileName, int loadMode, MagicFlags flags, string expected)
        {
            using (Magic magic = Magic.Open(flags))
            {
                byte[] magicBuffer;
                switch (loadMode)
                {
                    case 0:
                        magic.LoadMagicFile(TestSetup.MagicFile);
                        break;
                    case 1:
                        // Force .Net's unicode -> ansi encoding convert failure by using exotic/obscure characters
                        magic.LoadMagicFile(TestSetup.MagicUnicodeOnlyPath);
                        break;
                    case 2:
                        using (FileStream fs = new FileStream(TestSetup.MagicFile, FileMode.Open, FileAccess.Read))
                        {
                            magicBuffer = new byte[fs.Length];
                            fs.Read(magicBuffer, 0, magicBuffer.Length);
                        }
                        magic.LoadMagicBuffer(magicBuffer);
                        break;
                    case 3:
                        using (FileStream fs = new FileStream(TestSetup.MagicFile, FileMode.Open, FileAccess.Read))
                        {
                            magicBuffer = new byte[fs.Length];
                            fs.Read(magicBuffer, 0, magicBuffer.Length);
                        }
                        magic.LoadMagicBuffer(magicBuffer, 0, magicBuffer.Length);
                        break;
                }

                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

                // CheckFile
                string result = magic.CheckFile(sampleFile);
                Console.WriteLine($"{sampleFileName,-24}: [R] {result} [E] {expected}");
                Assert.IsTrue(result.Equals(expected, StringComparison.Ordinal));

                // CheckBuffer (Array)
                byte[] buffer;
                using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read))
                {
                    buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                }
                result = magic.CheckBuffer(buffer);
                Console.WriteLine($"{sampleFileName,-24}: [R] {result} [E] {expected}");
                Assert.IsTrue(result.Equals(expected, StringComparison.Ordinal));

                // CheckBuffer (Span)
                Span<byte> span;
                using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read))
                {
                    buffer = new byte[fs.Length];
                    span = buffer.AsSpan();
#if NETFRAMEWORK
                    fs.Read(buffer, 0, buffer.Length);
#else
                    fs.Read(span);
#endif
                }

                result = magic.CheckBuffer(span);
                Console.WriteLine($"{sampleFileName,-24}: [R] {result} [E] {expected}");
                Assert.IsTrue(result.Equals(expected, StringComparison.Ordinal));
            }
        }
    }
}
