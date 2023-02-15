# Patch for file 5.44

file 5.44 has issues to be compiled on MinGW-w64.

## pipe

MinGW-w64 does not offer linkable `pipe()`.

```
C:/msys64/mingw64/bin/../lib/gcc/x86_64-w64-mingw32/12.2.0/../../../../x86_64-w64-mingw32/bin/ld.exe: .libs/funcs.o: in function `file_pipe_closexec':
D:\Jang\Build\Source\PEBakery\Library\libmagic\file-5.43\src/funcs.c:850: undefined reference to `pipe'

```

You must patch `file-5.44` with `MinGW_w64_pipe_fix.diff`.

```sh
cd file-5.44
patch -p1 < $REPO/native/windows/patch-5.44/MinGW_w64_fcntl_fix.diff
```

## ioctl

Unless you configured `file-5.44` without any compression support (Ex xzlib, bzlib), the build will fail with error message complaining the linker cannot find `ioctl` function.

```
C:/msys64/mingw32/bin/../lib/gcc/i686-w64-mingw32/10.2.0/../../../../i686-w64-mingw32/bin/ld.exe: D:\Jang\Build\Source\PEBakery\Library\libmagic\file-5.40-mod\src/compress.c:417: undefined reference to `ioctl'
```

To use compression support capabilities of libmagic, you must apply `MinGW_w64_ioctl_fix.diff` to `file-5.44`.

```sh
cd file-5.44
patch -p1 < $REPO/native/windows/patch-5.44/MinGW_w64_ioctl_fix.diff
```
