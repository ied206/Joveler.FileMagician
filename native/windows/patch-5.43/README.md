# Patch for file 5.43

file 5.43 has three issues to be compiled on MinGW-w64.

## putc

`fname_print()` function has a path for a platform without a widechar support, such as MinGW-w64.

That path calls `putc` fucntion without any `FILE*` to print, and produces a compile error.

```
file.c: In function 'fname_print':
file.c:605:25: error: too few arguments to function 'putc'
  605 |                         putc(c);
      |                         ^~~~
In file included from file.h:79,
                 from file.c:32:
C:/msys64/mingw64/include/stdio.h:705:15: note: declared here
  705 |   int __cdecl putc(int _Ch,FILE *_File);
      |               ^~~~
```

You must patch `file-5.43` with `MinGW_w64_putc_fix.diff`.

## pipe

MinGW-w64 does not offer linkable `pipe()`.

```
C:/msys64/mingw64/bin/../lib/gcc/x86_64-w64-mingw32/12.2.0/../../../../x86_64-w64-mingw32/bin/ld.exe: .libs/funcs.o: in function `file_pipe_closexec':
D:\Jang\Build\Source\PEBakery\Library\libmagic\file-5.43\src/funcs.c:850: undefined reference to `pipe'

```

You must patch `file-5.43` with `MinGW_w64_pipe_fix.diff`.

```sh
cd file-5.40
patch -p1 < $REPO/native/windows/patch/MinGW_w64_fcntl_fix.diff
```

## ioctl

Unless you configured `file-5.40` with `--disable-xzlib` and `--disable-bzlib`, the build will fail with error message complaining the linker cannot find `ioctl` function.

```
C:/msys64/mingw32/bin/../lib/gcc/i686-w64-mingw32/10.2.0/../../../../i686-w64-mingw32/bin/ld.exe: D:\Jang\Build\Source\PEBakery\Library\libmagic\file-5.40-mod\src/compress.c:417: undefined reference to `ioctl'
```

To use `xz-utils`/`bzip2` capabilities of libmagic, you must apply `MinGW_w64_ioctl_fix.diff` to `file-5.40`.

```sh
cd file-5.40
patch -p1 < $REPO/native/windows/patch/MinGW_w64_ioctl_fix.diff
```
