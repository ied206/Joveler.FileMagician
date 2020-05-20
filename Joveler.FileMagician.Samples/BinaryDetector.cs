using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.FileMagician.Samples
{
    /// <summary>
    /// Wrapper of Joveler.FileMagician
    /// </summary>
    public class BinaryDetector : IDisposable
    {
        #region Fields and Properties
        private Magic _magic = null;
        private readonly object _magicLock = new object();
        private static bool _magicLoaded = false;
        #endregion

        #region Enums
        [Flags]
        public enum BinFlags : uint
        {
            None = 0x00,
            Executable = 0x01,
            Library = 0x02,
            // Bitness
            Bit16 = 0x100,
            Bit32 = 0x200,
            Bit64 = 0x400,
            // File Format
            PE = 0x10000,
            Elf = 0x20000,
            MachO = 0x40000,
        }

        [Flags]
        public enum PEFormatFlags : uint
        {
            None = 0,
            ExternalPdb = 1,
            Console = 2,
            Gui = 4,
            NetAssembly = 8,
        }

        [Flags]
        public enum ElfFormatFlags : uint
        {
            None = 0,
            Lsb = 1,
            Msb = 2,
            DynamicLink = 4,
            StaticLink = 8,
            Pie = 0x10,
            NotStrip = 0x20,
            Strip = 0x40,
            DebugInfo = 0x80,
        }

        [Flags]
        public enum MachOFormatFlags : uint
        {
            None = 0,
            Pie = 0x00200000u,

            // ReSharper disable CommentTypo
            /*
            MH_NOUNDEFS                = 0x00000001u,
            MH_INCRLINK                = 0x00000002u,
            MH_DYLDLINK                = 0x00000004u,
            MH_BINDATLOAD              = 0x00000008u,
            MH_PREBOUND                = 0x00000010u,
            MH_SPLIT_SEGS              = 0x00000020u,
            MH_LAZY_INIT               = 0x00000040u,
            MH_TWOLEVEL                = 0x00000080u,
            MH_FORCE_FLAT              = 0x00000100u,
            MH_NOMULTIDEFS             = 0x00000200u,
            MH_NOFIXPREBINDING         = 0x00000400u,
            MH_PREBINDABLE             = 0x00000800u,
            MH_ALLMODSBOUND            = 0x00001000u,
            MH_SUBSECTIONS_VIA_SYMBOLS = 0x00002000u,
            MH_CANONICAL               = 0x00004000u,
            MH_WEAK_DEFINES            = 0x00008000u,
            MH_BINDS_TO_WEAK           = 0x00010000u,
            MH_ALLOW_STACK_EXECUTION   = 0x00020000u,
            MH_ROOT_SAFE               = 0x00040000u,
            MH_SETUID_SAFE             = 0x00080000u,
            MH_NO_REEXPORTED_DYLIBS    = 0x00100000u,
            MH_PIE                     = 0x00200000u,
            MH_DEAD_STRIPPABLE_DYLIB   = 0x00400000u,
            MH_HAS_TLV_DESCRIPTORS     = 0x00800000u,
            MH_NO_HEAP_EXECUTION       = 0x01000000u,
            MH_APP_EXTENSION_SAFE = 0x02000000u
            */
            // ReSharper enable CommentTypo
        }

        public enum BinArch
        {
            Unknown = 0,
            IA16 = 0x20,
            IA32 = 0x21,
            Amd64 = 0x22,
            Arm = 0x40,
            Arm64 = 0x41,
            Mips32R2 = 0x60,
        }
        #endregion

        #region Constructor
        public BinaryDetector()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            LoadFileMagician(baseDir);

            string magicFile = Path.Combine(baseDir, "magic.mgc");
            _magic = Magic.Open(magicFile, MagicFlags.None);
        }
        #endregion

        #region Disposable Pattern
        ~BinaryDetector()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            lock (_magicLock)
            {
                if (_magic != null)
                {
                    _magic.Dispose();
                    _magic = null;
                }
            }
        }
        #endregion

        #region LoadFileMagician
        private static void LoadFileMagician(string baseDir)
        {
            if (!_magicLoaded)
            {
                const string x64 = "x64";
                const string x86 = "x86";
                const string armhf = "armhf";
                const string arm64 = "arm64";

                const string dllName = "libmagic-1.dll";
                const string soName = "libmagic.so";

                string libPath = null;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    switch (RuntimeInformation.ProcessArchitecture)
                    {
                        case Architecture.X86:
                            libPath = Path.Combine(baseDir, x86, dllName);
                            break;
                        case Architecture.X64:
                            libPath = Path.Combine(baseDir, x64, dllName);
                            break;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    switch (RuntimeInformation.ProcessArchitecture)
                    {
                        case Architecture.X64:
                            libPath = Path.Combine(baseDir, x64, soName);
                            break;
                        case Architecture.Arm:
                            libPath = Path.Combine(baseDir, armhf, soName);
                            break;
                        case Architecture.Arm64:
                            libPath = Path.Combine(baseDir, arm64, soName);
                            break;
                    }
                }

                if (libPath == null)
                    throw new PlatformNotSupportedException();

                Magic.GlobalInit(libPath);
                _magicLoaded = true;
            }
        }
        #endregion

        #region InspectBinary
        #region (Notes) References
        // ReSharper disable CommentTypo
        /*
        Some ELF executables are reported as 'shared object' when they are compiled as PIE to apply ASLR.
        https://askubuntu.com/questions/690631/executables-vs-shared-objects

        [amd64]
        avcodec-58.dll:          PE32+ executable (DLL) (console) x86-64 (stripped to external PDB), for MS Windows
        avutil-56.dll:           PE32+ executable (DLL) (console) x86-64 (stripped to external PDB), for MS Windows
        libavcodec.so.58.35.100: ELF 64-bit LSB shared object, x86-64, version 1 (SYSV), dynamically linked, BuildID[sha1]=fe8714fd2d5bc59dd6aeef16279ff983431e5c96, stripped
        libavutil.so.56.22.100:  ELF 64-bit LSB shared object, x86-64, version 1 (SYSV), dynamically linked, BuildID[sha1]=a912700373ab3024e9a0213e8e84ca6e947197ac, stripped
        // From Ubuntu 18.04
        file:                    ELF 64-bit LSB shared object, x86-64, version 1 (SYSV), dynamically linked, interpreter /lib64/l, for GNU/Linux 3.2.0, BuildID[sha1]=ba74252751fddf2ef1b1d3bd2098c95550eee976, stripped
        libmagic.so.1.0.0:       ELF 64-bit LSB shared object, x86-64, version 1 (SYSV), dynamically linked, BuildID[sha1]=b6e20c167093fac860f30abc696ec08b5d4328fc, stripped
        // From Windows 10
        notepad.exe:             PE32+ executable (GUI) x86-64, for MS Windows
        xcopy.exe:               PE32+ executable (console) x86-64, for MS Windows
        kernel32.dll:            PE32+ executable (DLL) (console) x86-64, for MS Windows
        // From Bandizip
        Bandizip/Ark32.dll:       PE32 executable (DLL) (GUI) Intel 80386, for MS Windows
        Bandizip/Ark32lgplv2.dll: PE32 executable (DLL) (GUI) Intel 80386, for MS Windows
        Bandizip/Ark64.dll:       PE32+ executable (DLL) (GUI) x86-64, for MS Windows
        Bandizip/Ark64lgplv2.dll: PE32+ executable (DLL) (GUI) x86-64, for MS Windows
        // From macOS Binary:
        filezilla:            Mach-O 64-bit x86_64 executable, flags:<NOUNDEFS|DYLDLINK|TWOLEVEL|WEAK_DEFINES|BINDS_TO_WEAK|PIE|HAS_TLV_DESCRIPTORS>
        libgnutls.30.dylib:   Mach-O 64-bit x86_64 dynamically linked shared library, flags:<NOUNDEFS|DYLDLINK|TWOLEVEL|WEAK_DEFINES|BINDS_TO_WEAK|NO_REEXPORTED_DYLIBS|HAS_TLV_DESCRIPTORS>
        libhogweed.4.5.dylib: Mach-O 64-bit x86_64 dynamically linked shared library, flags:<NOUNDEFS|DYLDLINK|TWOLEVEL|NO_REEXPORTED_DYLIBS>
        libidn.12.dylib:      Mach-O 64-bit x86_64 dynamically linked shared library, flags:<NOUNDEFS|DYLDLINK|TWOLEVEL|NO_REEXPORTED_DYLIBS>
        libjson-c.4.dylib:    Mach-O 64-bit x86_64 dynamically linked shared library, flags:<NOUNDEFS|DYLDLINK|TWOLEVEL|NO_REEXPORTED_DYLIBS|HAS_TLV_DESCRIPTORS>


        [i686]
        liblzma5.so:    ELF 32-bit LSB shared object, Intel 80386, version 1 (SYSV), dynamically linked, BuildID[sha1]=4d93d3d30f3fe79c8bdae247cab4df05b8659b52, stripped
        liblzma.dll:    PE32 executable (DLL) (console) Intel 80386 (stripped to external PDB), for MS Windows

        [armhf]                                                                                                                                                                                     130 ↵
        bash_454:       ELF 32-bit LSB executable, ARM, EABI5 version 1 (SYSV), dynamically linked, interpreter /lib/ld-, for GNU/Linux 3.14.57, with debug_info, not stripped
        libssl_454.so:  ELF 32-bit LSB shared object, ARM, EABI5 version 1 (SYSV), dynamically linked, with debug_info, not stripped

        [arm64]
        libmagic.so:    ELF 64-bit LSB shared object, ARM aarch64, version 1 (SYSV), dynamically linked, BuildID[sha1]=c6a63e381e3cae7d25bed8cf487f221c2f68a1dd, with debug_info, not stripped

        [mipsel]
        bash_454:       ELF 32-bit LSB executable, MIPS, MIPS32 rel2 version 1 (SYSV), dynamically linked, interpreter /lib/ld., for GNU/Linux 3.14.57, with debug_info, not stripped
        libssl_454.so:  ELF 32-bit LSB shared object, MIPS, MIPS32 rel2 version 1 (SYSV), dynamically linked, not stripped
        libssl_494.so:  ELF 32-bit LSB shared object, MIPS, MIPS32 rel2 version 1 (SYSV), dynamically linked, with debug_info, not stripped

        [C# (.Net Core)]
        Joveler.FileMagician.dll:       PE32 executable (DLL) (console) Intel 80386 Mono/.Net assembly, for MS Windows
        Joveler.FileMagician.Tests.dll: PE32 executable (console) Intel 80386 Mono/.Net assembly, for MS Windows

        */
        // ReSharper enable CommentTypo
        #endregion

        public struct BinInfo
        {
            public readonly string TypeStr;
            public readonly BinFlags BinFlags;
            public readonly BinArch BinArch;
            public readonly uint FormatFlags;

            public BinInfo(string typeStr, BinFlags binFlags, BinArch binArch, uint formatFlags)
            {
                TypeStr = typeStr;
                BinFlags = binFlags;
                BinArch = binArch;
                FormatFlags = formatFlags;
            }
        }

        public BinInfo InspectBinary(string binFilePath)
        {
            string typeStr;
            lock (_magicLock)
            {
                typeStr = _magic.CheckFile(binFilePath);
            }

            BinFlags binFlags = ParseBinFlags(typeStr);
            BinArch binArch = ParseBinArch(typeStr, binFlags);
            uint formatFlags = ParseFormatFlags(typeStr, binFlags);

            return new BinInfo(typeStr, binFlags, binArch, formatFlags);
        }

        private static BinArch ParseBinArch(string typeStr, BinFlags binFlags)
        {
            BinArch binArch = BinArch.Unknown;

            if (binFlags.HasFlag(BinFlags.PE))
            {
                if (typeStr.Contains("x86-64", StringComparison.Ordinal))
                    binArch = BinArch.Amd64;
                else if (typeStr.Contains("Intel 80386", StringComparison.Ordinal))
                    binArch = BinArch.IA32;
            }
            else if (binFlags.HasFlag(BinFlags.Elf))
            {
                if (typeStr.Contains("x86-64", StringComparison.Ordinal))
                    binArch = BinArch.Amd64;
                else if (typeStr.Contains("Intel 80386", StringComparison.Ordinal))
                    binArch = BinArch.IA32;
                else if (typeStr.Contains("ARM, EABI5", StringComparison.Ordinal))
                    binArch = BinArch.Arm;
                else if (typeStr.Contains("ARM aarch64", StringComparison.Ordinal))
                    binArch = BinArch.Arm64;
                else if (typeStr.Contains("MIPS, MIPS32 rel2", StringComparison.Ordinal))
                    binArch = BinArch.Mips32R2;
            }
            else if (binFlags.HasFlag(BinFlags.MachO))
            {
                if (typeStr.Contains("x86_64", StringComparison.Ordinal))
                    binArch = BinArch.Amd64;
            }

            return binArch;
        }

        private static BinFlags ParseBinFlags(string typeStr)
        {
            BinFlags binFlags = BinFlags.None;

            if (typeStr.StartsWith("PE32+ executable", StringComparison.Ordinal))
            { // Windows 64bit PE File (.exe, .dll)
                binFlags |= BinFlags.PE;
                binFlags |= BinFlags.Bit64;
            }
            else if (typeStr.StartsWith("PE32 executable", StringComparison.Ordinal))
            { // Windows 32bit PE File (.exe, .dll)
                binFlags |= BinFlags.PE;
                binFlags |= BinFlags.Bit32;
            }
            else if (typeStr.StartsWith("ELF 64-bit", StringComparison.Ordinal))
            { // Linux 64bit ELF File (.so, etc.)
                binFlags |= BinFlags.Elf;
                binFlags |= BinFlags.Bit64;
            }
            else if (typeStr.StartsWith("ELF 32-bit", StringComparison.Ordinal))
            { // Linux 32bit ELF File (.so, etc.)
                binFlags |= BinFlags.Elf;
                binFlags |= BinFlags.Bit32;
            }
            else if (typeStr.StartsWith("Mach-O 64-bit", StringComparison.Ordinal))
            { // macOS 64bit Mach-O File (.so, etc.)
                binFlags |= BinFlags.MachO;
                binFlags |= BinFlags.Bit64;
            }
            else if (typeStr.StartsWith("Mach-O 32-bit", StringComparison.Ordinal))
            { // macOS 32bit Mach-O File (.so, etc.)
                binFlags |= BinFlags.MachO;
                binFlags |= BinFlags.Bit32;
            }

            if (binFlags.HasFlag(BinFlags.PE))
            {
                if (typeStr.Contains("(DLL)", StringComparison.Ordinal))
                    binFlags |= BinFlags.Library;
                else
                    binFlags |= BinFlags.Executable;
            }
            else if (binFlags.HasFlag(BinFlags.Elf))
            {
                // ASLR-applied ELF executables (PIE) are reported as 'shared object'.
                // So do not rely on "exectuable" string, but on "interpreter" string.
                if (typeStr.Contains("interpreter", StringComparison.Ordinal))
                    binFlags |= BinFlags.Executable;
                else if (typeStr.Contains("shared object", StringComparison.Ordinal))
                    binFlags |= BinFlags.Library;
            }
            else if (binFlags.HasFlag(BinFlags.MachO))
            {
                if (typeStr.Contains("executable", StringComparison.Ordinal))
                    binFlags |= BinFlags.Executable;
                else if (typeStr.Contains("dynamically linked shared library", StringComparison.Ordinal))
                    binFlags |= BinFlags.Library;
            }

            return binFlags;
        }

        private static uint ParseFormatFlags(string typeStr, BinFlags binFlags)
        {
            uint valInt = 0;

            // .Net Core 2.1+ has special optimization for Enum.HasFlag(), no performance penalty
            if (binFlags.HasFlag(BinFlags.PE))
            {
                PEFormatFlags formatFlags = PEFormatFlags.None;

                // Does external PDB exist?
                if (typeStr.Contains("(stripped to external PDB)", StringComparison.Ordinal))
                    formatFlags |= PEFormatFlags.ExternalPdb;

                // Subsystem : Console or GUI
                if (typeStr.Contains("(console)", StringComparison.Ordinal))
                    formatFlags |= PEFormatFlags.Console;
                else if (typeStr.Contains("(GUI)", StringComparison.Ordinal))
                    formatFlags |= PEFormatFlags.Gui;
                Debug.Assert(!(formatFlags.HasFlag(PEFormatFlags.Console) &&
                             formatFlags.HasFlag(PEFormatFlags.Gui)));

                // Is .Net Assembly?
                if (typeStr.Contains("Mono/.Net assembly", StringComparison.Ordinal))
                    formatFlags |= PEFormatFlags.NetAssembly;
                Debug.Assert(formatFlags.HasFlag(PEFormatFlags.NetAssembly) &&
                             binFlags.HasFlag(BinFlags.Bit32));

                valInt = (uint)formatFlags;
            }
            else if (binFlags.HasFlag(BinFlags.Elf))
            {
                ElfFormatFlags formatFlags = ElfFormatFlags.None;

                // Endian : Little endian or Big endian?
                if (typeStr.Contains("-bit LSB", StringComparison.Ordinal))
                    formatFlags |= ElfFormatFlags.Lsb;
                else if (typeStr.Contains("-bit MSB", StringComparison.Ordinal))
                    formatFlags |= ElfFormatFlags.Msb;
                Debug.Assert(!(formatFlags.HasFlag(ElfFormatFlags.Lsb) &&
                               formatFlags.HasFlag(ElfFormatFlags.Msb)));

                // Link : Dynamic or Static?
                // Nearly every ELF binary is dynamic, due to glibc dependency.
                if (typeStr.Contains(", dynamically linked,", StringComparison.Ordinal))
                    formatFlags |= ElfFormatFlags.DynamicLink;
                else if (typeStr.Contains(", statically linked", StringComparison.Ordinal))
                    formatFlags |= ElfFormatFlags.StaticLink;
                Debug.Assert(!(formatFlags.HasFlag(ElfFormatFlags.DynamicLink) &&
                               formatFlags.HasFlag(ElfFormatFlags.StaticLink)));

                // PIE : Is position independant executable?
                if (binFlags.HasFlag(BinFlags.Executable))
                {
                    if (typeStr.Contains("shared object,", StringComparison.Ordinal))
                        formatFlags |= ElfFormatFlags.Pie;
                }

                // Strip : Is binary stripped?
                if (typeStr.Contains(", not stripped", StringComparison.Ordinal))
                    formatFlags |= ElfFormatFlags.NotStrip;
                else if (typeStr.Contains(", stripped", StringComparison.Ordinal))
                    formatFlags |= ElfFormatFlags.Strip;
                Debug.Assert(!(formatFlags.HasFlag(ElfFormatFlags.NotStrip) &&
                               formatFlags.HasFlag(ElfFormatFlags.Strip)));

                if (typeStr.Contains(", with debug_info", StringComparison.Ordinal))
                    formatFlags |= ElfFormatFlags.DebugInfo;

                valInt = (uint)formatFlags;
            }
            else if (binFlags.HasFlag(BinFlags.MachO))
            {
                MachOFormatFlags formatFlags = MachOFormatFlags.None;

                // PIE : Is position independant executable?
                if (typeStr.Contains("PIE", StringComparison.Ordinal))
                    formatFlags |= MachOFormatFlags.Pie;

                valInt = (uint)formatFlags;
            }

            return valInt;
        }
        #endregion
    }
}