# ChangeLog

## v2.x

### v2.3.0

Released on 2023-02-14

- Update included libmagic to 5.44.
- Provide Joveler.FileMagician.Cli program on release.
- Target .NET Core 3.1 instead of .NET Standard 2.1.
- Target .NET Framework 4.6 instead of discontinued .NET Framework 4.5.1.

### v2.2.0

Released on 2022-01-28

- Update included libmagic to 5.41.

### v2.1.0

Reelased on 2021-04-05

- Update included libmagic to 5.40.
- Official support for Windows ARM64.

### v2.0.0

Released on 2020-05-20

- Updated included libmagic to 5.38.
- Native libraries are now placed following [NuGet convention-based working directory](https://docs.microsoft.com/en-US/nuget/create-packages/creating-a-package#create-the-nuspec-file) on .NET Standard build.
- Redesign public APIs.
    - Rewrite `GetMagicPath()` into `GetDefaultMagicFilePath()`.
    - Rename `Load()` to `LoadMagicFile()`.
    - Rename `LoadBuffer()` to `LoadMagicBuffer()`.
    - Add `LoadMagicBuffers()` APIs.
    - Add `GetParam()`, `SetParam()` APIs.
    - Rewrite version functions into properties.

## v1.x

### v1.3.1

Released on 2019-11-01

- Improve RHEL/CentOS compatibility.

### v1.3.0

Released on 2019-10-20

- Updated included libmagic to 5.37 from 5.36.
- Support the macOS platform.
- Apply improved native library loader, [Joveler.DynLoader](https://github.com/ied206/Joveler.DynLoader).

### v1.2.1

Released on 2019-05-08

- Better fix for Unicode path handling on Windows.

### v1.2.0

Released on 2019-05-03

- Fix Unicode problem on Windows with buffer-based APIs.

### v1.1.0

Released on 2019-04-18

- New APIs with `Span<T>`.

### v1.0.0

Released on 2019-04-15

- Initial release.
- Includes libmagic 5.36 binaries for Windows/Linux.
