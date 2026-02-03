# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-01-30

### Added
- Initial release of ZenohDotNet.Unity package
- Session, Publisher, Subscriber support with UniTask integration
- Query/Queryable (request-response pattern)
- Main thread callback dispatching using `UniTask.Post()`
- Unity 2021.2+ support with .NET Standard 2.1
- Assembly definitions for Runtime and Editor
- Automatic dependency on com.cysharp.unitask (UPM)
- Dependency on ZenohDotNet.Native (NuGet for Unity)

### Features
- Async/await support via UniTask
- `UniTask.RunOnThreadPool()` for background operations
- Automatic main thread marshalling for callbacks
- Cancellation token support with `GetCancellationTokenOnDestroy()`
- Synchronous `Publisher.Put()` for Unity Update() loop
- Full cross-platform support (Windows, Linux, macOS on x64 and ARM64)

### Dependencies
- ZenohDotNet.Native >= 0.1.0 (via NuGet for Unity)
- com.cysharp.unitask >= 2.5.4 (via UPM)
