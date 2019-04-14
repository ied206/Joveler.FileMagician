# Usage

## Initialization

`Joveler.FileMagician` requires explicit loading of an libmagic library.

You must call `Magic.GlobalInit()` before using `Joveler.FileMagician`.

Put this snippet in your application's init code:

```cs
public static void InitNativeLibrary()
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
                libPath = Path.Combine(x86, dllName);
                break;
            case Architecture.X64:
                libPath = Path.Combine(x64, dllName);
                break;
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                libPath = Path.Combine(x64, soName);
                break;
            case Architecture.Arm:
                libPath = Path.Combine(armhf, soName);
                break;
            case Architecture.Arm64:
                libPath = Path.Combine(arm64, soName);
                break;
        }
    }

    if (libPath == null)
        throw new PlatformNotSupportedException();

    Magic.GlobalInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

`Joveler.FileMagician` comes with sets of binaries of `libmagic 5.36` and its file signature database.  
They will be copied into the build directory at build time.

| Platform         | Binary                         | License                 |
|------------------|--------------------------------|-------------------------|
| Windows x86      | `$(OutDir)\x86\libmagic-1.dll` | 2-Clause BSD (w LGPLv2 libiconv) |
| Windows x64      | `$(OutDir)\x64\libmagic-1.dll` | 2-Clause BSD (w LGPLv2 libiconv) |
| Ubuntu 18.04 x64 | `$(OutDir)\x64\libmagic.so`    | 2-Clause BSD |
| Debian 9 armhf   | `$(OutDir)\armhf\libmagic.so`  | 2-Clause BSD |
| Debian 9 arm64   | `$(OutDir)\arm64\libmagic.so`  | 2-Clause BSD |

File signature database will be copied to `$(OutDir)\magic.mgc`.

### Custom binary

To use custom libmagic binary instead, call `Magic.GlobalInit()` with a path to the custom binary.

#### NOTES

- Create an empty file named `Joveler.FileMagician.Lib.Exclude` in project directory to prevent copy of package-embedded binary.
- Create an empty file named `Joveler.FileMagician.Mgc.Exclude` in project directory to prevent copy of package-embedded file signature database.
- libmagic depends on libiconv (included) in Windows and zlib (not included) in linux.
- You may have to compile custom libmagic to use ManagedWimLib in untested linux distribution.

### Cleanup

To unload libmagic library explicitly, call `Magic.GlobalCleanup()`.

## API

`Joveler.FileMagician` provides sets of APIs match to its original.

Most of the use cases follow this flow.

1. Create Magic instance with `Magic.Open("magic.mgc")`.
2. Do your job by calling API of your interest.
3. Cleanup Magic instance with the Disposable pattern.

[Joveler.FileMagician.Tests](./Joveler.FileMagician.Tests) provides a lot of examples of how to use `Joveler.FileMagician`.
