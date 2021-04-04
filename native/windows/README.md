# Compile libmagic.dll

To compile libmagic on Windows, you should use MSYS2.

## Required Tools

- [MSYS2](https://www.msys2.org/)
    - Install `base-devel`, `mingw-w64-x86_64-toolchain`, `mingw-w64-i686-toolchain` packages.
- [libmagic source](http://www.darwinsys.com/file/)
- [llvm-mingw](https://github.com/mstorsjo/llvm-mingw)
    - Binaries compiled from MSYS2 MinGW 32bit compiler depends on `libgcc_s_dw2-1.dl` and `libwinpthread-1.dll`. Compiling with clang avoids this problem.
    - MSYS2 does not provide ARM64 gcc toolchain, so instead use clang to cross-compile ARM64 binary.

## Manual

1. Open MSYS2 shell.
    | Arch  | MSYS2 shell       | Native or Cross? |
    |-------|-------------------|------------------|
    | x86   | MSYS2 MinGW 32bit | native compile   |
    | x64   | MSYS2 MinGW 64bit | native compile   |
    | arm64 | MSYS2 MinGW 64bit | cross compile    |
1. Extract the `libmagic` source code.
1. Apply patches to `libmagic` source if needed.
    - Refer to [patch README](patch\README.md) to when and why the patches are necessary.
1. Run `libmagic-msys2.sh`.
    - You are recommended to pass a path of `llvm-mingw` to compile x86/x64 binaries.
    - You must pass a path of `llvm-mingw` to compile ARM64 binaries.
    ```
    [Examples]
    x86: ./libmagic-msys2.sh -a i686 -t /c/llvm-mingw /d/build/native/file-5.40 
    x64: ./libmagic-msys2.sh -a x86_64 -t /c/llvm-mingw /d/build/native/file-5.40 
    aarch64: ./libmagic-msys2.sh -a aarch64 -t /c/llvm-mingw /d/build/native/file-5.40 
    ```
1. Gather binaries from `build-<arch>` directory.

## Compiler Behavior Diffence

MinGW-w64 and Clang behaves differently on processing wildcards in arguments.

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
