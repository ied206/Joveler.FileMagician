# Usage

## Initialization

`Joveler.FileMagician` requires explicit loading of a libmagic library.

You must call `Magic.GlobalInit()` before using `Joveler.FileMagician`.

Put this snippet in your application's init code:

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

    string libPath = null;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        libPath = Path.Combine(arch, "libmagic-1.dll");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libPath = Path.Combine(arch, "libmagic.so");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libPath = Path.Combine(arch, "libmagic.dylib");

    if (libPath == null || !File.Exists(libPath))
        throw new PlatformNotSupportedException();

    Magic.GlobalInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

`Joveler.FileMagician` comes with sets of binaries of `libmagic 5.37` and its file signature database.  
They will be copied into the build directory at build time.

| Platform         | Binary                         | License                 |
|------------------|--------------------------------|-------------------------|
| Windows x86      | `$(OutDir)\x86\libmagic-1.dll` | 2-Clause BSD (w LGPLv2 `libiconv-2.dll`) |
| Windows x64      | `$(OutDir)\x64\libmagic-1.dll` | 2-Clause BSD (w LGPLv2 `libiconv-2.dll`) |
| Ubuntu 18.04 x64 | `$(OutDir)\x64\libmagic.so`    | 2-Clause BSD |
| Debian 9 armhf   | `$(OutDir)\armhf\libmagic.so`  | 2-Clause BSD |
| Debian 9 arm64   | `$(OutDir)\arm64\libmagic.so`  | 2-Clause BSD |
| macOS 10.15      | `$(OutDir)\x64\libmagic.dylib` | 2-Clause BSD |

File signature database will be copied to `$(OutDir)\magic.mgc`.

### Custom binary

To use custom libmagic binary instead, call `Magic.GlobalInit()` with a path to the custom binary.

#### NOTES

- Create an empty file named `Joveler.FileMagician.Lib.Exclude` in the project directory to prevent copy of the package-embedded binary.
- Create an empty file named `Joveler.FileMagician.Mgc.Exclude` in the project directory to prevent copy of package-embedded file signature database.
- libmagic depends on libiconv (included) in Windows and zlib (not included) in Linux.
- If you call `Magic.GlobalInit()` without `libPath` parameter on Linux, it will search for system-installed libmagic.
  - Linux binaries are not portable. They may not work on your distribution. In that case, call parameter-less `Magic.GlobalInit()` to use system-installed libmagic.

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

This overload loads magic database automatically.

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
void Load(string magicFile);
void LoadBuffer(byte[] magicBuffer, int offset, int count);
void LoadBuffer(ReadOnlySpan<byte> magicSpan);
```

### Check type of data

You can check the type of a file or buffer through these methods.

```csharp
string CheckFile(string inName);
string CheckBuffer(byte[] buffer, int offset, int count);
string CheckBuffer(ReadOnlySpan<byte> span);
```

### Getting and setting MagicFlags

You can get current MagicFlags or set new MagicFlags through these methods.

```csharp
MagicFlags GetFlags();
void SetFlags(MagicFlags flags);
```
