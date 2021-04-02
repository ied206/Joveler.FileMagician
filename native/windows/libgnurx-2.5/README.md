# libgnurx for Windows

The code was adopted from https://github.com/nscaife/file-windows

## Original README.md from @nscaife

The contents of this directory are from http://ftp.gnome.org/pub/gnome/binaries/win32/dependencies/.

I have made a few modifications to the Makefile to disable creation of gnurx.lib. The creation of this file relies on lib.exe from Visual Studio (forcing a Windows build environment), and is not needed to build libmagic or file.

## LICENSE

libgnurx-2.5 is licensed under LGPLv2.1. 
