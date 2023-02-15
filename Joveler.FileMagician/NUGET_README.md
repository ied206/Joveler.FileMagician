# Joveler.FileMagician

Joveler.FileMagician is a cross-platform [libmagic](http://www.darwinsys.com/file/) pinvoke library for .NET.

libmagic is a file-type guesser library that powers POSIX's file command.

## Usage

Refer to the [project webpage](https://github.com/ied206/Joveler.FileMagician).

## Support

### Targeted .NET platforms

- .NET Core 3.1
- .NET Standard 2.0
- .NET Framework 4.6

### Supported OS platforms

| Platform | Architecture | Tested |
|----------|--------------|--------|
| Windows  | x86          | Yes    |
|          | x64          | Yes    |
|          | arm64        | Yes    |
| Linux    | x64          | Yes    |
|          | armhf        | Yes    |
|          | arm64        | Yes    |
| macOS    | x64          | Yes    |
|          | arm64        | Yes    |

#### Tested Linux distributions

| Architecture | Distribution | Note |
|--------------|--------------|------|
| x64          | Ubuntu 20.04 | Tested on AppVeyor CI         |
| armhf        | Debian 11    | Emulated on QEMU's virt board |
| arm64        | Debian 11    | Emulated on QEMU's virt board |

### Tested libmagic version

- 5.44 (Included)
