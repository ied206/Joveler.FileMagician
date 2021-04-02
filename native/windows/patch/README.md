# Patch for file 5.40

file 5.40 has two issues to be compiled on MinGW-w64.

## fcntl

Windows SDK nor MinGW-w64 do not define `F_SETFD`, thus breaking the build.

```
funcs.c:820:22: error: 'F_SETFD' undeclared (first use in this function)
  820 |  (void)fcntl(fds[0], F_SETFD, FD_CLOEXEC);
      |                      ^~~~~~~
funcs.c:820:22: note: each undeclared identifier is reported only once for each function it appears in
funcs.c: In function 'file_clear_closexec':
funcs.c:828:19: error: 'F_SETFD' undeclared (first use in this function)
  828 |  return fcntl(fd, F_SETFD, 0);
      |                   ^~~~~~~
```

You must patch `file-5.40` with `MinGW_w64_fcntl_fix.diff`.

```sh
cd file-5.40
patch -p1 < $REPO/native/windows/patch/MinGW_w64_fcntl_fix.diff
```

## ioctl

Unless you configured `file-5.40` with `--disable-xzlib` and `--disable-bzlib`, the build will fail with error message complaining the linkder cannot find `ioctl` function.

```
C:/msys64/mingw32/bin/../lib/gcc/i686-w64-mingw32/10.2.0/../../../../i686-w64-mingw32/bin/ld.exe: D:\Jang\Build\Source\PEBakery\Library\libmagic\file-5.40-mod\src/compress.c:417: undefined reference to `ioctl'
```

To use `xz-utils`/`bzip2` capabilities of libmagic, you must apply `MinGW_w64_ioctl_fix.diff` to `file-5.40`.

```sh
cd file-5.40
patch -p1 < $REPO/native/windows/patch/MinGW_w64_ioctl_fix.diff
```
