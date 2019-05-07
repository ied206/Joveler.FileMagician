# Joveler.FileMagician

<div style="text-align: left">
    <img src="./Image/Logo.svg" height="128">
</div>

C# pinvoke library for native [libmagic](http://www.darwinsys.com/file/).

libmagic is a file type guesser library which powers POSIX's file command.

| Branch    | Build Status   |
|-----------|----------------|
| Master    | [![CI Master Branch Build Status](https://ci.appveyor.com/api/projects/status/nc4uwfscb470dm9b/branch/master?svg=true)](https://ci.appveyor.com/project/ied206/joveler-filemagician/branch/master) |
| Develop   | [![CI Develop Branch Build Status](https://ci.appveyor.com/api/projects/status/nc4uwfscb470dm9b/branch/develop?svg=true)](https://ci.appveyor.com/project/ied206/joveler-filemagician/branch/develop) |

## Install

Joveler.FileMagician can be installed via [nuget](https://www.nuget.org/packages/Joveler.FileMagician/).

[![NuGet](https://buildstats.info/nuget/Joveler.FileMagician)](https://www.nuget.org/packages/Joveler.FileMagician)

## Support

### Targeted .Net platforms

- .Net Framework 4.5.1
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

### Supported OS platforms

| Platform | Architecture | Tested |
|----------|--------------|--------|
| Windows  | x86          | Yes    |
|          | x64          | Yes    |
| Linux    | x64          | Yes    |
|          | armhf        | Yes    |
|          | arm64        | Yes    |

**Note:** I want to support macOS, but I do not have any macOS device. Please contribute to macOS support!

#### Tested linux distributions

| Architecture | Distribution | Note |
|--------------|--------------|------|
| x64          | Ubuntu 18.04 | Tested on AppVeyor CI         |
| armhf        | Debian 9     | Emulated on QEMU's virt board |
| arm64        | Debian 9     | Emulated on QEMU's virt board |

### Supported libmagic version

- 5.36 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

`Joveler.FileMagician` and `libmagic` is licensed under the 2-Clause BSD license.  
Bundled Windows binary also contains LGPLv2 libiconv.  
See [LICENSE](./LICENSE) for details.

The logo is licensed under Apache 2.0 License.  
[Search icon](https://material.io/tools/icons/?icon=search&style=baseline) from the Material Icons.

