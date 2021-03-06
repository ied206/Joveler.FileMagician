# Joveler.FileMagician

<div style="text-align: left">
    <img src="./Image/Logo.svg" height="128">
</div>

Cross-platform [libmagic](http://www.darwinsys.com/file/) pinvoke library for .NET.

libmagic is a file type guesser library which powers POSIX's file command.

| CI Server       | Branch  | Build Status   |
|-----------------|---------|----------------|
| AppVeyor        | Master  | [![CI Master Branch Build Status](https://ci.appveyor.com/api/projects/status/nc4uwfscb470dm9b/branch/master?svg=true)](https://ci.appveyor.com/project/ied206/joveler-filemagician/branch/master) |
| AppVeyor        | Develop | [![CI Develop Branch Build Status](https://ci.appveyor.com/api/projects/status/nc4uwfscb470dm9b/branch/develop?svg=true)](https://ci.appveyor.com/project/ied206/joveler-filemagician/branch/develop) |
| Azure Pipelines | Master  | [![Azure Pipelines CI Master Branch Build Status](https://ied206.visualstudio.com/Joveler.FileMagician/_apis/build/status/ied206.Joveler.FileMagician?branchName=master)](https://dev.azure.com/ied206/Joveler.FileMagician/_build) |
| Azure Pipelines | Develop | [![Azure Pipelines CI Develop Branch Build Status](https://ied206.visualstudio.com/Joveler.FileMagician/_apis/build/status/ied206.Joveler.FileMagician?branchName=develop)](https://dev.azure.com/ied206/Joveler.FileMagician/_build) |

## Install

Joveler.FileMagician can be installed via [nuget](https://www.nuget.org/packages/Joveler.FileMagician/).

[![NuGet](https://buildstats.info/nuget/Joveler.FileMagician)](https://www.nuget.org/packages/Joveler.FileMagician)

## Support

### Targeted .NET platforms

- .NET Standard 2.1 (.NET Core 3.0+)
- .NET Standard 2.0 (.NET Framework 4.6.1+, .NET Core 2.0+)
- .NET Framework 4.5.1

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

macOS arm64 should be supported on theory, but I do not have access to an Apple Sillicon device to test. Please contribute if you have an ARM64 macOS machine.

#### Tested Linux distributions

| Architecture | Distribution | Note |
|--------------|--------------|------|
| x64          | Ubuntu 18.04 | Tested on AppVeyor CI         |
| armhf        | Debian 10    | Emulated on QEMU's virt board |
| arm64        | Debian 10    | Emulated on QEMU's virt board |

### Supported libmagic version

- 5.40 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## License

- `Joveler.FileMagician` and `libmagic` is licensed under the BSD 2-Clause license. 
- Bundled Windows binary depends on LGPLv2.1 libgnurx. 
- See [LICENSE](./LICENSE) for details.
- The logo, [search icon](https://material.io/tools/icons/?icon=search&style=baseline) from the Material Icons, is licensed under Apache 2.0 License.
