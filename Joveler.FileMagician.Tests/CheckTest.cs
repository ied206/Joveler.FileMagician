using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class CheckTest
    {
        private struct TypeInfo
        {
            public readonly string FileType;
            public readonly string MimeType;
            public readonly string MimeEncoding;

            public TypeInfo(string fileType, string mimeType, string mimeEncoding)
            {
                FileType = fileType;
                MimeType = mimeType;
                MimeEncoding = mimeEncoding;
            }
        }

        private readonly Dictionary<string, TypeInfo> _fileTypeDict = new Dictionary<string, TypeInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["ANSI.txt"] = new TypeInfo("ASCII text, with no line terminators", "text/plain", "us-ascii"),
            ["EUC-KR.txt"] = new TypeInfo("ISO-8859 text, with no line terminators", "text/plain", "iso-8859-1"),
            ["RTF.rtf"] = new TypeInfo("Rich Text Format data, version 1, ANSI", "text/rtf", "binary"),
            ["Samples.7z"] = new TypeInfo("7-zip archive data, version 0.3", "application/x-7z-compressed", "binary"),
            ["Samples.zip"] = new TypeInfo("Zip archive data, at least v2.0 to extract", "application/zip", "binary"),
            ["UTF16BE_EN_wBOM.txt"] = new TypeInfo("Big-endian UTF-16 Unicode text, with no line terminators", "text/plain", "utf-16be"),
            ["UTF16BE_KR_wBOM.txt"] = new TypeInfo("Big-endian UTF-16 Unicode text, with no line terminators", "text/plain", "utf-16be"),
            ["UTF16LE_EN_wBOM.txt"] = new TypeInfo("Little-endian UTF-16 Unicode text, with no line terminators", "text/plain", "utf-16le"),
            ["UTF16LE_KR_wBOM.txt"] = new TypeInfo("Little-endian UTF-16 Unicode text, with no line terminators", "text/plain", "utf-16le"),
            ["UTF8_EN_wBOM.txt"] = new TypeInfo("UTF-8 Unicode (with BOM) text, with no line terminators", "text/plain", "utf-8"),
            ["UTF8_EN_woBOM.txt"] = new TypeInfo("ASCII text, with no line terminators", "text/plain", "us-ascii"),
            ["UTF8_KR_wBOM.txt"] = new TypeInfo("UTF-8 Unicode (with BOM) text, with no line terminators", "text/plain", "utf-8"),
            ["UTF8_KR_woBOM.txt"] = new TypeInfo("UTF-8 Unicode text, with no line terminators", "text/plain", "utf-8"),
        };

        [TestMethod]
        public void FileType()
        {
            foreach ((string sampleFileName, TypeInfo ti) in _fileTypeDict)
            {
                Template(sampleFileName, MagicFlags.NONE, ti.FileType);
            }
        }

        [TestMethod]
        public void MimeType()
        {
            foreach ((string sampleFileName, TypeInfo ti) in _fileTypeDict)
            {
                Template(sampleFileName, MagicFlags.MIME_TYPE, ti.MimeType);
            }
        }

        [TestMethod]
        public void MimeEncoding()
        {
            foreach ((string sampleFileName, TypeInfo ti) in _fileTypeDict)
            {
                Template(sampleFileName, MagicFlags.MIME_ENCODING, ti.MimeEncoding);
            }
        }

        public void Template(string sampleFileName, MagicFlags flags, string expected)
        {
            using (Magic magic = Magic.Open(TestSetup.MagicFile, flags))
            {
                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

                // CheckFile
                string result = magic.CheckFile(sampleFile);
                Assert.IsTrue(result.Equals(expected, StringComparison.Ordinal));

                // CheckBuffer (Array)
                byte[] buffer;
                using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read))
                {
                    buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                }
                result = magic.CheckBuffer(buffer);
                Assert.IsTrue(result.Equals(expected, StringComparison.Ordinal));

                // CheckBuffer (Span)
                Span<byte> span;
                using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read))
                {
                    buffer = new byte[fs.Length];
                    span = buffer.AsSpan();
                    fs.Read(span);
                }
                result = magic.CheckBuffer(span);
                Assert.IsTrue(result.Equals(expected, StringComparison.Ordinal));
            }
        }
    }
}
