/*
    C# pinvoke wrapper written by Hajin Jang
    Copyright (C) 2019-2022 Hajin Jang

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

namespace Joveler.FileMagician
{
    #region Enums
    [Flags]
    public enum MagicFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0x0000000,
        /// <summary>
        /// Turn on debugging
        /// </summary>
        Debug = 0x0000001,
        /// <summary>
        /// Follow symlinks
        /// </summary>
        Symlink = 0x0000002,
        /// <summary>
        /// Check inside compressed files
        /// </summary>
        Compress = 0x0000004,
        /// <summary>
        /// Look at the contents of devices
        /// </summary>
        Devices = 0x0000008,
        /// <summary>
        /// Return the MIME type
        /// </summary>
        MimeType = 0x0000010,
        /// <summary>
        /// Return all matches
        /// </summary>
        Continue = 0x0000020,
        /// <summary>
        /// Print warnings to stderr
        /// </summary>
        Check = 0x0000040,
        /// <summary>
        /// Restore access time on exit
        /// </summary>
        PreserveATime = 0x0000080,
        /// <summary>
        /// Don't convert unprintable chars
        /// </summary>
        Raw = 0x0000100,
        /// <summary>
        /// Handle ENOENT etc as real errors
        /// </summary>
        Error = 0x0000200,
        /// <summary>
        /// Return the MIME encoding 
        /// </summary>
        MimeEncoding = 0x0000400,
        Mime = MimeType | MimeEncoding,
        /// <summary>
        /// Return the Apple creator/type
        /// </summary>
        Apple = 0x0000800,
        /// <summary>
        /// Return a /-separated list of extensions
        /// </summary>
        Extension = 0x1000000,
        /// <summary>
        /// Check inside compressed files but not report compression
        /// </summary>
        CompressTransp = 0x2000000,
        NoDesc = Extension | Mime | Apple,
        /// <summary>
        /// Don't check for compressed files
        /// </summary>
        NoCheckCompress = 0x0001000,
        /// <summary>
        /// Don't check for tar files
        /// </summary>
        NoCheckTar = 0x0002000,
        /// <summary>
        /// Don't check magic entries
        /// </summary>
        NoCheckSoft = 0x0004000,
        /// <summary>
        /// Don't check magic entries
        /// </summary>
        NoCheckAppType = 0x0008000,
        /// <summary>
        /// Don't check for elf details
        /// </summary>
        NoCheckElf = 0x0010000,
        /// <summary>
        /// Don't check for text files
        /// </summary>
        NoCheckText = 0x0020000,
        /// <summary>
        /// Don't check for cdf files
        /// </summary>
        NoCheckCdf = 0x0040000,
        /// <summary>
        /// Don't check for CSV files
        /// </summary>
        NoCheckCsv = 0x0080000,
        /// <summary>
        /// Don't check tokens
        /// </summary>
        NoCheckTokens = 0x0100000,
        /// <summary>
        /// Don't check text encodings
        /// </summary>
        NoCheckEncoding = 0x0200000,
        /// <summary>
        /// Don't check for JSON files
        /// </summary>
        NoCheckJson = 0x0400000,
        /// <summary>
        /// No built-in tests; only consult the magic file
        /// </summary>
        NoCheckBuiltin = NoCheckCompress | NoCheckTar | NoCheckAppType |
                         NoCheckElf | NoCheckText | NoCheckCdf | NoCheckCsv |
                         NoCheckTokens | NoCheckEncoding | NoCheckJson,
    }

    public enum MagicParam
    {
        /// <summary>
        /// Get and set uint16_t (ushort) integer value.
        /// </summary>
        InDirMax = 0,
        /// <summary>
        /// Controls the maximum number of calls for name/use.
        /// Get and set uint16_t (ushort) integer value.
        /// </summary>
        NameMax = 1,
        /// <summary>
        /// Controls how many ELF program sections will be processed.
        /// Get and set uint16_t (ushort) integer value.
        /// </summary>
        ElfPhNumMax = 2,
        /// <summary>
        /// Controls how many ELF sections will be processed.
        /// Get and set uint16_t (ushort) integer value.
        /// </summary>
        ElfShNumMax = 3,
        /// <summary>
        /// Controls how many ELF sections will be processed.
        /// Get and set uint16_t (ushort) integer value.
        /// </summary>
        ElfNotesMax = 4,
        /// <summary>
        /// Get and set uint16_t (ushort) integer value.
        /// </summary>
        RegexMax = 5,
        /// <summary>
        /// Number of bytes to read from file.
        /// Get and set size_t (ulong) integer value.
        /// </summary>
        BytesMax = 6,
    }
    #endregion
}
