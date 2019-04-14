using System;
// ReSharper disable UnusedMember.Global

namespace Joveler.FileMagician
{
    #region Enums
    [Flags]
    public enum MagicFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        NONE = 0x0000000,
        /// <summary>
        /// Turn on debugging
        /// </summary>
        DEBUG = 0x0000001,
        /// <summary>
        /// Follow symlinks
        /// </summary>
        SYMLINK = 0x0000002,
        /// <summary>
        /// Check inside compressed files
        /// </summary>
        COMPRESS = 0x0000004,
        /// <summary>
        /// Look at the contents of devices
        /// </summary>
        DEVICES = 0x0000008,
        /// <summary>
        /// Return the MIME type
        /// </summary>
        MIME_TYPE = 0x0000010,
        /// <summary>
        /// Return all matches
        /// </summary>
        CONTINUE = 0x0000020,
        /// <summary>
        /// Print warnings to stderr
        /// </summary>
        CHECK = 0x0000040,
        /// <summary>
        /// Restore access time on exit
        /// </summary>
        PRESERVE_ATIME = 0x0000080,
        /// <summary>
        /// Don't convert unprintable chars
        /// </summary>
        RAW = 0x0000100,
        /// <summary>
        /// Handle ENOENT etc as real errors
        /// </summary>
        ERROR = 0x0000200,
        /// <summary>
        /// Return the MIME encoding 
        /// </summary>
        MIME_ENCODING = 0x0000400,
        MIME = MIME_TYPE | MIME_ENCODING,
        /// <summary>
        /// Return the Apple creator/type
        /// </summary>
        APPLE = 0x0000800,
        /// <summary>
        /// Return a /-separated list of extensions
        /// </summary>
        EXTENSION = 0x1000000,
        /// <summary>
        /// Check inside compressed files but not report compression
        /// </summary>
        COMPRESS_TRANSP = 0x2000000,
        NODESC = EXTENSION | MIME | APPLE,
        /// <summary>
        /// Don't check for compressed files
        /// </summary>
        NO_CHECK_COMPRESS = 0x0001000,
        /// <summary>
        /// Don't check for tar files
        /// </summary>
        NO_CHECK_TAR = 0x0002000,
        /// <summary>
        /// Don't check magic entries
        /// </summary>
        NO_CHECK_SOFT = 0x0004000,
        /// <summary>
        /// Don't check magic entries
        /// </summary>
        NO_CHECK_APPTYPE = 0x0008000,
        /// <summary>
        /// Don't check for elf details
        /// </summary>
        NO_CHECK_ELF = 0x0010000,
        /// <summary>
        /// Don't check for text files
        /// </summary>
        NO_CHECK_TEXT = 0x0020000,
        /// <summary>
        /// Don't check for cdf files
        /// </summary>
        NO_CHECK_CDF = 0x0040000,
        /// <summary>
        /// Don't check tokens
        /// </summary>
        NO_CHECK_TOKENS = 0x0100000,
        /// <summary>
        /// Don't check text encodings
        /// </summary>
        NO_CHECK_ENCODING = 0x0200000,
        /// <summary>
        /// Don't check for JSON files
        /// </summary>
        NO_CHECK_JSON = 0x0400000,
        /// <summary>
        /// No built-in tests; only consult the magic file
        /// </summary>
        NO_CHECK_BUILTIN = NO_CHECK_COMPRESS | NO_CHECK_TAR | NO_CHECK_APPTYPE |
                           NO_CHECK_ELF | NO_CHECK_TEXT | NO_CHECK_CDF |
                           NO_CHECK_TOKENS | NO_CHECK_ENCODING | NO_CHECK_JSON,
    }

    public enum MagicParam
    {
        INDIR_MAX = 0,
        NAME_MAX = 1,
        ELF_PHNUM_MAX = 2,
        ELF_SHNUM_MAX = 3,
        ELF_NOTES_MAX = 4,
        REGEX_MAX = 5,
        BYTES_MAX = 6,
    }
    #endregion
}
