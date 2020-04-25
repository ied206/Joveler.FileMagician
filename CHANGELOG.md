# ChangeLog

## v2.x

### v2.0.0

Released in 2020-04-24

- Updated included libmagic to 5.38.
- Redesign public APIs.
    - Change `GetMagicPath()` into `GetDefaultMagicFilePath()`.
    - Exception-related APIs are now private.
    - 

## v1.x

### v1.3.1

Released in 2019-11-01

- Improve RHEL/CentOS compatibility.

### v1.3.0

Released in 2019-10-20

- Updated included libmagic to 5.37 from 5.36.
- Support macOS platform.
- Apply improved native library loader, [Joveler.DynLoader](https://github.com/ied206/Joveler.DynLoader).

### v1.2.1

Released in 2019-05-08

- More proper fix for unicode path handling on Windows.

### v1.2.0

Released in 2019-05-03

- Fix Unicode problem on Windows with buffer-based APIs.

### v1.1.0

Released in 2019-04-18

- New APIs with `Span<T>`.

### v1.0.0

Released in 2019-04-15

- Initial release.
- Includes libmagic 5.36 binaries for Windows/Linux.
