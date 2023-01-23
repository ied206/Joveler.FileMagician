/*
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
            // Hancom Office
            ["HWP97.hwp"] = new TypeInfo("Hangul (Korean) Word Processor File 3.0", "application/octet-stream", "binary"),
            ["HWP2016.hwp"] = new TypeInfo("Hangul (Korean) Word Processor File 5.x", "application/x-hwp", "binary", "hwp"),
            ["HWP2016.hwpx"] = new TypeInfo("Zip data (MIME type \"application/hwp+zip\"?)", "application/zip", "binary"),
            ["Hancell2016.cell"] = new TypeInfo("Microsoft Excel 2007+", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "binary", "xlsx"),
            ["Hanshow2016.show"] = new TypeInfo("Microsoft PowerPoint 2007+", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "binary", "pptx"),
            // LibreOffice 6.0.7.3
            ["LibreCalc6.ods"] = new TypeInfo("OpenDocument Spreadsheet", "application/vnd.oasis.opendocument.spreadsheet", "binary", "ods"),
            ["LibreImpress6.odp"] = new TypeInfo("OpenDocument Presentation", "application/vnd.oasis.opendocument.presentation", "binary", "odp"),
            ["LibreWriter6.odt"] = new TypeInfo("OpenDocument Text", "application/vnd.oasis.opendocument.text", "binary", "odt"),
            // Micorsoft Office 2003
            ["Office2003.doc"] = new TypeInfo("Composite Document File V2 Document, Little Endian, Os: Windows, Version 10.0, Code page: 949, Author: Joveler Jang, Template: Normal.dotm, Last Saved By: Jang Joveler, Revision Number: 2, Name of Creating Application: Microsoft Office Word, Create Time/Date: Mon Jan 23 06:26:00 2023, Last Saved Time/Date: Mon Jan 23 06:26:00 2023, Number of Pages: 1, Number of Words: 7, Number of Characters: 40, Security: 0", "application/msword", "binary", "doc/dot/"),
            ["Office2003.ppt"] = new TypeInfo("Composite Document File V2 Document, Little Endian, Os: Windows, Version 10.0, Code page: 949, Title: Microsoft Office Powerpoint 2019, Author: Jang Joveler, Last Saved By: Jang Joveler, Revision Number: 2, Name of Creating Application: Microsoft Office PowerPoint, Total Editing Time: 08:07, Create Time/Date: Sat Apr 20 04:54:43 2019, Last Saved Time/Date: *Bad* 0x38ec24c063cdaa29, Number of Words: 8", "application/vnd.ms-powerpoint", "binary", "ppt/pps/pot"),
            ["Office2003.xls"] = new TypeInfo("Composite Document File V2 Document, Little Endian, Os: Windows, Version 10.0, Code page: 949, Name of Creating Application: Microsoft Excel, Create Time/Date: Fri Jun  5 18:19:34 2015, Last Saved Time/Date: Mon Jan 23 06:27:21 2023, Security: 0", "application/vnd.ms-excel", "binary", "xls/xlt"),
            // Microsoft Office 2019
            ["Office2019.docx"] = new TypeInfo("Microsoft Word 2007+", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "binary", "docx"),
            ["Office2019.pptx"] = new TypeInfo("Microsoft PowerPoint 2007+", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "binary", "pptx"),
            ["Office2019.xlsx"] = new TypeInfo("Microsoft Excel 2007+", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "binary", "xlsx"),
            // Archive Format
            ["Samples.7z"] = new TypeInfo("7-zip archive data, version 0.3", "application/x-7z-compressed", "binary", "7z/cb7"),
            ["Samples.tar"] = new TypeInfo("POSIX tar archive (GNU)", "application/x-tar", "binary", "tar/gtar"),
            ["Samples.tar.bz2"] = new TypeInfo("bzip2 compressed data, block size = 900k", "application/x-bzip2", "binary", "bz2"),
            ["Samples.tar.xz"] = new TypeInfo("XZ compressed data, checksum CRC64", "application/x-xz", "binary", "xz"),
            ["Samples.zip"] = new TypeInfo("Zip archive data, at least v2.0 to extract, compression method=deflate", "application/zip", "binary"),
            ["Samples.alz"] = new TypeInfo("ALZ archive data", "application/octet-stream", "binary", "alz"),
            ["Samples.egg"] = new TypeInfo("EGG archive data, version 1.0", "application/octet-stream", "binary", "egg"),
            ["Samples.rar"] = new TypeInfo("RAR archive data, v4, os: Win32", "application/x-rar", "binary", "rar/cbr"),
            ["Samples.rar5"] = new TypeInfo("RAR archive data, v5", "application/x-rar", "binary", "rar"),
            // Image Format
            ["Logo.bmp"] = new TypeInfo("PC bitmap, Windows 3.x format, 128 x 128 x 4, image size 8192, cbSize 8310, bits offset 118", "image/bmp", "binary", "bmp"),
            ["Logo.bpg"] = new TypeInfo("BPG (Better Portable Graphics)", "image/bpg", "binary"),
            ["Logo.jpg"] = new TypeInfo("JPEG image data, JFIF standard 1.01, aspect ratio, density 1x1, segment length 16, baseline, precision 8, 128x128, components 3", "image/jpeg", "binary", "jpeg/jpg/jpe/jfif"),
            ["Logo.png"] = new TypeInfo("PNG image data, 128 x 128, 8-bit/color RGBA, non-interlaced", "image/png", "binary", "png"),
            ["Logo.svg"] = new TypeInfo("SVG Scalable Vector Graphics image", "image/svg+xml", "us-ascii", "svg"),
            ["Logo.webp"] = new TypeInfo("RIFF (little-endian) data, Web/P image", "image/webp", "binary", "webp"),
            // Database + Unicode-only path test 
            ["DB.sqlite"] = new TypeInfo("SQLite 3.x database, last written using SQLite version 3027002, file counter 2, database pages 2, 1st free page 2, free pages 1, cookie 0x2, schema 4, UTF-8, version-valid-for 2", "application/vnd.sqlite3", "binary", "sqlite/sqlite3/db/db3/dbe/sdb/help"),
            ["ᄒᆞᆫ글ḀḘ韓國.sqlite"] = new TypeInfo("SQLite 3.x database, last written using SQLite version 3027002, file counter 2, database pages 2, 1st free page 2, free pages 1, cookie 0x2, schema 4, UTF-8, version-valid-for 2", "application/vnd.sqlite3", "binary", "sqlite/sqlite3/db/db3/dbe/sdb/help"),
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
                        magic.LoadMagicFile(TestSetup.MagicCompiledFile);
                        break;
                    case 1:
                        // Force .NET's unicode -> ansi encoding convert failure by using exotic/obscure characters
                        magic.LoadMagicFile(TestSetup.MagicCompiledUnicodeOnlyPath);
                        break;
                    case 2:
                        using (FileStream fs = new FileStream(TestSetup.MagicCompiledFile, FileMode.Open, FileAccess.Read))
                        {
                            magicBuffer = new byte[fs.Length];
                            fs.Read(magicBuffer, 0, magicBuffer.Length);
                        }
                        magic.LoadMagicBuffer(magicBuffer);
                        break;
                    case 3:
                        magic.LoadMagicFile(TestSetup.MagicSourceFile);
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
