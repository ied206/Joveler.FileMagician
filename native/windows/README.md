# Compile libmagic.dll

To compile libmagic on Windows, you should use MSYS2 and llvm-mingw.

## Required Tools

- [MSYS2](https://www.msys2.org/)
    - Install `base-devel` and `autotools` packages.
- [libmagic source](http://www.darwinsys.com/file/)
- [llvm-mingw](https://github.com/mstorsjo/llvm-mingw)
    - llvm-mingw supports x86, x64 and arm64 in one toolchain, including cross-compiling.
    - llvm-mingw links to UCRT by default.
    - Compiling with clang avoids these problems:
        - MSYS2 CLANGARM64 toolchain does not support cross-compiling from amd64 machines.
        - Binaries compiled from MSYS2 MinGW 32bit compiler depends on `libgcc_s_dw2-1.dl` and `libwinpthread-1.dll`. 
        - Modern .NET runtimes use `UCRT`, while MSYS2 MinGW links to `msvcrt`. Mixing two different C runtimes is not recommended.

## Manual

1. Open MSYS2 shell. Any MINGW/CLANG shell can be used.
1. Extract the `libmagic` source code.
1. Apply patches to the `libmagic` source if needed.
    - Refer to [patch README](patch-5.45\README.md) to when and why the patches are necessary.
1. Run `libmagic-msys2.sh`.
    - You are recommended to pass a path of `llvm-mingw` to compile x86/x64 binaries.
    - You must pass a path of `llvm-mingw` to compile ARM64 binaries.
    ```
    [Examples]
    x86: ./libmagic-msys2.sh -a i686 -t /c/llvm-mingw /d/build/native/file-5.45
    x64: ./libmagic-msys2.sh -a x86_64 -t /c/llvm-mingw /d/build/native/file-5.45
    aarch64: ./libmagic-msys2.sh -a aarch64 -t /c/llvm-mingw /d/build/native/file-5.45
    ```
1. Gather binaries from the `build-<arch>` directory.

## Dependency

- libgnurx 2.5.1, source retrieved from [Fedora repository](https://src.fedoraproject.org/repo/pkgs/mingw32-libgnurx/)

## Compiler Behavior Difference

MinGW-w64 and Clang behave differently on processing wildcards in arguments.

| Compiler  | Wildcard Behavior |
|-----------|-------------------|
| MinGW-w64 | Interprets wildcards (`*`, `?`) to simulate bash behavior |
| Clang     | Pass arguments as-is |

However, it does not have any effect on dll functions.

### Examples

- MinGW-w64
    - argv[0]: `file`, argv[1]: `file.exe`, argv[2]: `libgnurx-0.dll`, argv[3]: `libmagic-1.dll`
    ```
    > file *
    file.exe:       PE32+ executable (console) x86-64 (stripped to external PDB), for MS Windows
    libgnurx-0.dll: PE32+ executable (DLL) (console) x86-64 (stripped to external PDB), for MS Windows
    libmagic-1.dll: PE32+ executable (DLL) (console) x86-64 (stripped to external PDB), for MS Windows
    ```
- Clang
    - argv[0]: `file`, argv[1]: `*`
    ```
    > file *
    *: cannot open `*' (No such file or directory)
    ```
