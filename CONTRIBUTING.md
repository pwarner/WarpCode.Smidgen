# Contributing to Smidgen

Thank you for considering contributing to Smidgen! Here are some guidelines to help you get started.

## Getting set up
You'll need the **.NET 11 SDK**, which builds and tests both targets (.NET 10 and .NET 11). If you also want to run the .NET 10 tests locally, you'll need the .NET 10 runtime too.

```shell
dotnet build   # warnings are errors, and code style is enforced
dotnet test    # runs the tests on both target frameworks
```

The tests use xUnit on the Microsoft Testing Platform, so filter with `--filter-class` / `--filter-method` rather than VSTest's `--filter`:

```shell
dotnet test --project tests/Smidgen.Tests --filter-class "WarpCode.Smidgen.Tests.IdGeneratorRangeQueryTests"
```

## How to Contribute
1. Create a new branch from `main` for your feature or bug fix.
2. Keep the build clean. Code style comes from `.editorconfig`, and its rules fail the build, so run `dotnet format` to fix most of them automatically. Files use LF line endings with a final newline; CI checks this with `dotnet format whitespace --folder --verify-no-changes`.
3. Write tests for your changes. All features and fixed bugs must have tests, and tests must be deterministic: use the fakes in `tests/Smidgen.Tests/TestHelpers.cs` rather than the real clock or real randomness. Aim for 100% coverage of the library (`dotnet test --coverage --coverage-output-format cobertura` writes a report to `TestResults/`).
4. Keep the docs in step. The [readme](readme.md) is the package's documentation on NuGet, so update it whenever behaviour or the public API changes.
5. Add a line describing your change under **[Unreleased]** in [CHANGELOG.md](CHANGELOG.md).
6. Write clear, concise commit messages, and submit a pull request with a detailed description of your changes. CI builds, tests, measures coverage and checks formatting on every push and pull request.

[AGENTS.md](AGENTS.md) describes the ID layout, the generation and monotonicity algorithms, and the testing conventions in more detail. Read it before changing `IdGenerator` or `EntropyProvider`.

## Releases
Releases are made by the maintainer: publishing a GitHub Release whose tag is the version (for example `2.0.0`) builds, tests and publishes the package to NuGet. The release notes come from [CHANGELOG.md](CHANGELOG.md).

## Code of Conduct
Follow the [Contributor Covenant](https://www.contributor-covenant.org), version 2.1.
By participating in this project, you agree to abide by its terms.

## Reporting Issues
If you encounter a bug or have a feature request, please open an issue on GitHub.
