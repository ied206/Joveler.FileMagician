# Joveler.FileMagic

<div style="text-align: left">
    <img src="./Image/Logo.svg" height="128">
</div>

C# pinvoke library for native [libmagic](http://www.darwinsys.com/file/).

libmagic is a file type guesser library which POSIX's file command is powered by.

| Branch    | Build Status   |
|-----------|----------------|
| Master    | [![CI Master Branch Build Status](https://ci.appveyor.com/api/projects/status/nc4uwfscb470dm9b/branch/master?svg=true)](https://ci.appveyor.com/project/ied206/joveler-filemagician/branch/master) |
| Develop   | [![CI Develop Branch Build Status](https://ci.appveyor.com/api/projects/status/nc4uwfscb470dm9b/branch/develop?svg=true)](https://ci.appveyor.com/project/ied206/joveler-filemagician/branch/develop) |

## Install

TODO

## Support

### Targeted .Net platforms

- .Net Framework 4.5.1
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

### Supported OS platforms

| Platform | Architecture | Tested |
|----------|--------------|--------|
| Windows  | x86, x64     | Yes    |
| Linux    | x64, armhf   | Yes    |
|          | arm64        | Yes    |

#### Tested linux distributions

| Architecture | Distribution | Note |
|--------------|--------------|------|
| x64          | Ubuntu 18.04 |      |
| armhf        | Debian 9     | Emulated on QEMU's virt board |
| arm64        | Debian 9     | Emulated on QEMU's virt board |

### Supported libmagic version

- 5.36 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

Licensed under the 2-Clause BSD license.  
See [LICENSE](./LICENSE) for details.

Logo is licensed under Apache 2.0 License.  
[Search icon](https://material.io/tools/icons/?icon=search&style=baseline) from the Material Icons.

