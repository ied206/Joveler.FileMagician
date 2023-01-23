# Joveler.FileMagician

Cross-platform [libmagic](http://www.darwinsys.com/file/) pinvoke library for .NET.

libmagic is a file type guesser library that powers POSIX's file command.

## Support

### Targeted .NET platforms

- .NET Core 3.1
- .NET Standard 2.0
- .NET Framework 4.6

### Supported OS platforms

| Platform | Architecture | Tested |
|----------|--------------|--------|
| Windows  | x86          | Yes    |
|          | x64          | Yes    |
|          | arm64        | Yes    |
| Linux    | x64          | Yes    |
|          | armhf        | Yes    |
|          | arm64        | Yes    |
| macOS    | x64          | Yes    |
|          | arm64        | Yes    |

#### Tested Linux distributions

| Architecture | Distribution | Note |
|--------------|--------------|------|
| x64          | Ubuntu 20.04 | Tested on AppVeyor CI         |
| armhf        | Debian 11    | Emulated on QEMU's virt board |
| arm64        | Debian 11    | Emulated on QEMU's virt board |

### Tested libmagic version

- 5.44 (Included)

# Usage

## Initialization

`Joveler.FileMagician` requires explicit loading of a libmagic library.

You must call `Magic.GlobalInit()` before using `Joveler.FileMagician`. Please put this code snippet in your application init code:

#### On .NET Framework

```cs
public static void InitNativeLibrary()
{
    string arch = null;
    switch (RuntimeInformation.ProcessArchitecture)
    {
        case Architecture.X86:
            arch = "x86";
            break;
        case Architecture.X64:
            arch = "x64";
            break;
        case Architecture.Arm:
            arch = "armhf";
            break;
        case Architecture.Arm64:
            arch = "arm64";
            break;
    }
    string libPath = Path.Combine(arch, "libmagic-1.dll");

    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    Magic.GlobalInit(libPath);
}
```

#### On .NET Standard, .NET Core

```cs
public static void InitNativeLibrary()
{
    string libBaseDir = AppDomain.CurrentDomain.BaseDirectory;
    string libDir = "runtimes";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        libDir = Path.Combine(libDir, "win-");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libDir = Path.Combine(libDir, "linux-");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libDir = Path.Combine(libDir, "osx-");

    switch (RuntimeInformation.ProcessArchitecture)
    {
        case Architecture.X86:
            libDir += "x86";
            break;
        case Architecture.X64:
            libDir += "x64";
            break;
        case Architecture.Arm:
            libDir += "arm";
            break;
        case Architecture.Arm64:
            libDir += "arm64";
            break;
    }
    libDir = Path.Combine(libDir, "native");

    // Some platforms require native library custom path to be an absolute path.
    string libPath = null;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        libPath = Path.Combine(libBaseDir, libDir, "libmagic-1.dll");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libPath = Path.Combine(libBaseDir, libDir, "libmagic.so");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libPath = Path.Combine(libBaseDir, libDir, "libmagic.dylib");

    if (libPath == null)
        throw new PlatformNotSupportedException($"Unable to find native library.");
    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    Magic.GlobalInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binaries

`Joveler.FileMagician` comes with sets of binaries of `libmagic 5.44` and its file signature database. They will be copied into the build directory at build time.

File signature database is copied to `$(OutDir)\magic.mgc` (compiled), and `$(OutDir)\magic.src` (source). Set up `Copy to output directory` property of those files to `PreserveNewest` or `None` following your need.

#### For .NET Framework

| Platform         | Binary                         | License                                    | C Runtime     |
|------------------|--------------------------------|--------------------------------------------|---------------|
| Windows x86      | `$(OutDir)\x86\libmagic-1.dll` | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) | Universal CRT |
| Windows x64      | `$(OutDir)\x64\libmagic-1.dll` | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) | Universal CRT |

- Bundled Windows binaries now target [Universal CRT](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170) for better interopability with MSVC.
    - UCRT is installed on Windows 10 by default, so action is required.
    - Windows Vista, 7 or 8.1 users may require [manual installation](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170) of UCRT.
- Create an empty file named `Joveler.FileMagician.Lib.Exclude` in the project directory to prevent copying of the package-embedded binary.
- Create an empty file named `Joveler.FileMagician.Mgc.Exclude` in the project directory to prevent copying of the package-embedded file signature database.
- libmagic depends on libgnurx (included) on Windows, which is covered by LGPLv2.1.

#### For .NET Standard & .NET Core

| Platform           | Binary                                        | License                                    | C Runtime     |
|--------------------|-----------------------------------------------|--------------------------------------------|---------------|
| Windows x86        | `$(OutDir)\runtimes\win-x86\libmagic-1.dll`   | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) | Universal CRT |
| Windows x64        | `$(OutDir)\runtimes\win-x64\libmagic-1.dll`   | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) | Universal CRT |
| Windows arm64      | `$(OutDir)\runtimes\win-x64\libmagic-1.dll`   | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) | Universal CRT |
| Ubuntu 20.04 x64   | `$(OutDir)\runtimes\linux-x64\libmagic.so`    | 2-Clause BSD                               | glibc         | 
| Debian 11 armhf    | `$(OutDir)\runtimes\linux-arm\libmagic.so`    | 2-Clause BSD                               | glibc         |
| Debian 11 arm64    | `$(OutDir)\runtimes\linux-arm64\libmagic.so`  | 2-Clause BSD                               | glibc         |
| macOS Big Sur x64  | `$(OutDir)\runtimes\osx-x64\libmagic.dylib`   | 2-Clause BSD                               | libSystem     |
| macOS Ventura x64  | `$(OutDir)\runtimes\osx-arm64\libmagic.dylib` | 2-Clause BSD                               | libSystem     |

- If you call `Magic.GlobalInit()` without `libPath` parameter on Linux or macOS, it will search for system-installed libmagic.
- libmagic was built without any compression support (e.g. `--disable-zlib`, `--disable-xzlib`, etc).
- Bundled Windows binaires now target [Universal CRT](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170) for better interopability with MSVC.
    - .NET Core/.NET 5+ runs on UCRT, so no action is required in most cases.
    - If you encounter a dependency issue on Windows Vista, 7 or 8.1, try [installing UCRT manually](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170).
- libmagic depends on libgnurx (included) on Windows, which is covered by LGPLv2.1.
- Linux binaries are not portable. They may not work on your distribution. In that case, call parameter-less `Magic.GlobalInit()` to use system-installed libmagic.

### Custom binary

To use the custom libmagic binary instead, call `Magic.GlobalInit()` with a path to the custom binary.

### Cleanup

To unload the libmagic library explicitly, call `Magic.GlobalCleanup()`.

## API

`Joveler.FileMagician` provides the `Magic` class, a wrapper of `libmagic`.

The `Magic` class implements the disposable pattern, so do not forget to clean up resources with the `using` keyword.

```csharp
string result;
using (Magic magic = Magic.Open("magic.mgc"))
{
    result = magic.CheckFile("target.7z");
}
// Prints "7-zip archive data, version 0.3"
Console.WriteLine(result);
```

[Joveler.FileMagician.Tests](./Joveler.FileMagician.Tests) also provides a lot of examples of how to use `Joveler.FileMagician`.

### Create an instance

`Magic.Open()` methods create an instance of `Magic` class.

```csharp
static Magic Open();
```

This overload loads the magic database automatically. It supports both a compiled magic database and a database source.

```csharp
static Magic Open(string magicFile);
```

You can also set `MagicFlags` automatically with these overloads.

`MagicFlags` controls which elements (filetype, mimetype, extension, etc) `libmagic` should detect on file buffers.

```csharp
static Magic Open(MagicFlags flags);
static Magic Open(string magicFile, MagicFlags flags);
```

### Load magic database

Magic database must be loaded first after creating a `Magic` instance unless you loaded it with `Magic.Open()`.

- `LoadMagicFile()` supports both a compiled magic database and a database source.
- `LoadMagicBuffer()` supports only a compiled magic database.

```csharp
void LoadMagicFile(string magicFile);
void LoadMagicBuffer(byte[] magicBuffer, int offset, int count);
void LoadMagicBuffer(ReadOnlySpan<byte> magicSpan);
void LoadMagicBuffers(IEnumerable<byte[]> magicBufs);
void LoadMagicBuffers(IEnumerable<ArraySegment<byte>> magicBufs);
```

### Check the type of data

Check the type of file or buffer.

```csharp
string CheckFile(string inName);
string CheckBuffer(byte[] buffer, int offset, int count);
string CheckBuffer(ReadOnlySpan<byte> span);
```

### Manage MagicFlags

Get or set `MagicFlags`, which directs the behavior of a type matcher and a format of type matching result.

```csharp
MagicFlags GetFlags();
void SetFlags(MagicFlags flags);
```

### Manage MagicParam (ADVANCED USERS ONLY)

Get or set the value of the `MagicParam`, which tweaks the internal parameters of the libmagic.

```csharp
enum MagicParam { ... }
ulong GetParam(MagicParam param);
void SetParam(MagicParam param, ulong value);
```

### Check the Version of the libmagic

Retrieve the version of the currently loaded libmagic.

```csharp
static Version Version { get; }
static int VersionInt { get; }
```

### Compile Magic Database

Compile a magic database from a database source. Useful when you need a custom magic database.

**WARNING:** Note that two-parameter override uses temp directory in the middle. `libmagic` does not offer creating a compiled database into a custom path, so it has to use a workaround.

```csharp
// Compiled database will be written into magicDestFile.
void Compile(string magicSrcFile, string magicDestFile);
// Compiled database will be written into $"{magicSrcFile}.mgc".
void Compile(string magicSrcFile);
```
