# Usage

## Initialization

`Joveler.FileMagician` requires explicit loading of a libmagic library.

You must call `Magic.GlobalInit()` before using `Joveler.FileMagician`. Please put this code snippet in your application init code:

#### For .NET Framework 4.5.1+

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

#### For .NET Standard 2.0+:

```cs
public static void InitNativeLibrary()
{
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

    string libPath = null;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        libPath = Path.Combine(libDir, "libmagic-1.dll");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libPath = Path.Combine(libDir, "libmagic.so");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libPath = Path.Combine(libDir, "libmagic.dylib");

    if (libPath == null)
        throw new PlatformNotSupportedException($"Unable to find native library.");
    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    Magic.GlobalInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

`Joveler.FileMagician` comes with sets of binaries of `libmagic 5.40` and its file signature database. They will be copied into the build directory at build time.

File signature database is copied to `$(OutDir)\magic.mgc`.

#### For .NET Framework 4.5.1+

| Platform         | Binary                         | License                 |
|------------------|--------------------------------|-------------------------|
| Windows x86      | `$(OutDir)\x86\libmagic-1.dll` | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) |
| Windows x64      | `$(OutDir)\x64\libmagic-1.dll` | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) |

- Create an empty file named `Joveler.FileMagician.Lib.Exclude` in the project directory to prevent copying of the package-embedded binary.
- Create an empty file named `Joveler.FileMagician.Mgc.Exclude` in the project directory to prevent copying of the package-embedded file signature database.
- libmagic depends on libgnurx (included) and some MinGW runtime libraries (included) on Windows. 
    - MinGW support libraries are covered by GPLv3 with [GCC RUNTIME LIBRARY EXCEPTION](https://www.gnu.org/licenses/gcc-exception-3.1.html)*. 

*[GCC RUNTIME LIBRARY EXCEPTION](https://www.gnu.org/licenses/gcc-exception-3.1.html) frees you and your software from GPLv3 obligations. 

#### For .NET Standard 2.0+

| Platform           | Binary                                       | License                 |
|--------------------|----------------------------------------------|-------------------------|
| Windows x86        | `$(OutDir)\runtimes\win-x86\libmagic-1.dll`  | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) |
| Windows x64        | `$(OutDir)\runtimes\win-x64\libmagic-1.dll`  | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) |
| Windows arm64      | `$(OutDir)\runtimes\win-x64\libmagic-1.dll`  | 2-Clause BSD (w LGPLv2.1 `libgnurx-0.dll`) |
| Ubuntu 18.04 x64   | `$(OutDir)\runtimes\linux-x64\libmagic.so`   | 2-Clause BSD |
| Debian 10 armhf    | `$(OutDir)\runtimes\linux-arm\libmagic.so`   | 2-Clause BSD |
| Debian 10 arm64    | `$(OutDir)\runtimes\linux-arm64\libmagic.so` | 2-Clause BSD |
| macOS Catalina x64 | `$(OutDir)\runtimes\osx-x64\libmagic.dylib`  | 2-Clause BSD |

- If you call `Magic.GlobalInit()` without `libPath` parameter on Linux or macOS, it will search for system-installed libmagic.
- Linux binaries are not portable. They may not work on your distribution. In that case, call parameter-less `Magic.GlobalInit()` to use system-installed libmagic.
- libmagic depends on zlib (not included) on Linux.

### Custom binary

To use custom libmagic binary instead, call `Magic.GlobalInit()` with a path to the custom binary.

### Cleanup

To unload libmagic library explicitly, call `Magic.GlobalCleanup()`.

## API

`Joveler.FileMagician` provides `Magic` class, a wrapper of `libmagic`.

`Magic` class implements the disposable pattern, so do not forget to clean up resources with `using` keyword.

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

This overload loads the magic database automatically.

```csharp
static Magic Open(string magicFile);
```

You can also set `MagicFlags` automatically with these overloads.

```csharp
static Magic Open(MagicFlags flags);
static Magic Open(string magicFile, MagicFlags flags);
```

### Load magic database

Magic database must be loaded first after creating a `Magic` instance unless you loaded it with `Magic.Open()`.

```csharp
void LoadMagicFile(string magicFile);
void LoadMagicBuffer(byte[] magicBuffer, int offset, int count);
void LoadMagicBuffer(ReadOnlySpan<byte> magicSpan);
void LoadMagicBuffers(IEnumerable<byte[]> magicBufs);
void LoadMagicBuffers(IEnumerable<ArraySegment<byte>> magicBufs);
```

### Check type of data

Check the type of a file or buffer.

```csharp
string CheckFile(string inName);
string CheckBuffer(byte[] buffer, int offset, int count);
string CheckBuffer(ReadOnlySpan<byte> span);
```

### Manage MagicFlags

Get or set `MagicFlags`, which directs the behavior of type matcher and a format of type matching result.

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

### Check Version of the libmagic

Retrieve version of currently loaded libmagic.

```csharp
static Version Version;
static int VersionInt;
```
