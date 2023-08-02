# Patch for file 5.45

MinGW-w64 and llvm-mingw can both compile static file 5.45.

Dynamic compilation requires some patching.

## Win32 DLL symbol exports in LLVM/Clang

libmagic needs to be patched to explicitly state `dllexport` in public symbols.

Apply this patch to `file-5.45`.

```sh
cd file-5.45
patch -p1 < $REPO/native/windows/patch-5.45/Win32_visibility_dllexport.diff
```

### What this patch does?

libmagic defines these macros in POSIX. 

| Macro | In POSIX | In Win32 | Intended Behavior |
|-------|----------|----------|-------------------|
| `file_public` | `__attribute__ ((__visibility__("default")))` | Undefined | Exports a function symbol. |
| `file_protected` | `__attribute__ ((__visibility__("hidden")))` | Undefined | States a function symbol should not be exported. |

This patch adds equivalent visibility directives for Win32.

| Macro | In POSIX | In Win32 | Intended Behavior |
|-------|----------|----------|-------------------|
| `file_public` | `__attribute__ ((__visibility__("default")))` | `__declspec(dllexport)` | Exports a function symbol. |
| `file_protected` | `__attribute__ ((__visibility__("hidden")))` | Empty | States a function symbol should not be exported. |

### Visibility of function symbols without any explicit directive

POSIX and Win32 have different behavior regarding the visiibility of function symbols without any explicit directive.

- In POSIX, it is decided by the toolchain.
- In Win32, MSVC has strict policy while MinGW-w64 or LLVM/Clang are somewhat relaxed.
    - MSVC requires explicit `dllexport` to make a function symbol to be exported in the DLL symbol export table (EAT).
    - MSYS2 MinGW-w64 has been exposing every function, at least in `libmagic-1.dll`.
    - llvm-mingw follows MSVC policy.
        - llvm-mingw had the same policy as MinGW-w64, but it seems to be changed in 2023.

### Solution

To solve this issue, declare the `file_public` macro as `__declspec(dllexport)` in the Win32 environment.
