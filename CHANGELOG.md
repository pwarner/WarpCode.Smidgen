# Changelog

All notable changes to WarpCode.Smidgen are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- A symbol package (`.snupkg`) published to the NuGet symbol server, and deterministic SourceLink builds, so you can step into the library's source while debugging.

### Changed
- **Targets .NET 10 and .NET 11.** .NET 8 is no longer supported.
- The readme and package description now state uniqueness accurately. One `IdGenerator` instance never repeats an ID. Separate instances can collide, and the readme's new **Uniqueness Across Instances** section shows how the chance grows with load. Earlier docs called cross-instance collisions negligible, which is true only at very low rates.

## [1.0.1] - 2026-04-12

### Changed
- The NuGet package now includes the `LICENSE` file.
- The readme's installation instructions point to the NuGet package.

## [1.0.0] - 2026-04-12

The first release.

- `IdGenerator`, which generates 60-bit, time-ordered, monotonic IDs stored in a `long`: a 40-bit time component (~6.5 ms precision, ~228 years from the epoch) and 20 bits of cryptographic entropy.
  - `NextId()` for a numeric ID, `NextString()` for a Crockford Base32 string such as `0HMK-QRST-1234`, and `NextString(template, placeholder)` for a custom format.
  - Lock-free and thread-safe, with no allocations in the hot path.
  - Configurable `Epoch`, `TimeProvider` and `EntropyProvider`, for custom epochs and deterministic tests.
- `EntropyProvider`, a thread-safe, buffered source of cryptographic randomness, with a `Shared` instance.
- Parsing: `IdGenerator.ParseRawString`, `ParseFormattedString` and their `TryParse…` versions. Decoding accepts the confusable characters `I`, `L` and `O`.
- Timestamps: `ExtractDateTime` from an ID, a raw string or a formatted string.
- Range queries: `GetMinId`, `GetMaxId` and `GetIdRange`, plus string versions (`GetMinRawString`, `GetRawStringRange`, `GetFormattedStringRange` and others), for `BETWEEN`-style database queries.
- Targets .NET 8 and .NET 10.

[Unreleased]: https://github.com/pwarner/WarpCode.Smidgen/compare/1.0.1...HEAD
[1.0.1]: https://github.com/pwarner/WarpCode.Smidgen/compare/1.0.0...1.0.1
[1.0.0]: https://github.com/pwarner/WarpCode.Smidgen/releases/tag/1.0.0
