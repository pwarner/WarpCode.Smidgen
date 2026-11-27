## What this project is
- An open-source library, published to NuGet as `WarpCode.Smidgen`, that generates 60-bit, time-ordered, monotonic identifiers stored in a `long` (Int64). It currently targets .NET 10 and .NET 11.
- IDs combine a 40-bit time component with 20 bits of cryptographic entropy, and are formatted as human-readable strings with Crockford Base32.
- Only types and methods that need to be public are public. Public types and methods must be documented. Classes are sealed until there is a reason not to.

## General
- Code style comes from .editorconfig, and its enforced rules are build errors (`EnforceCodeStyleInBuild` + `TreatWarningsAsErrors`). `dotnet format` fixes most of them.
- Use the latest compatible language syntax and a functional style where appropriate (for example: ternary expressions, switch expressions, expression-bodied members). The extension methods use C# 14 extension members.
- Performance is paramount: no allocations in the hot path (`stackalloc`, spans), lock-free generation with `Interlocked`, and `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on hot methods.
- Keep the public API surface minimal, for stability.
- The [readme](readme.md) is the consumer documentation and is packed into the NuGet package. It is first-class: keep it up to date with any change, and keep its examples compiling against the real API.
- Add a line describing every notable change under **[Unreleased]** in [CHANGELOG.md](CHANGELOG.md).

## ID format (60 bits in a 64-bit long)

```
┌─────────┬────────────────────────────┬──────────────────────┐
│ 4 bits  │         40 bits            │       20 bits        │
│ Unused  │    Time (Interval)         │    Entropy           │
│ (0000)  │  (elapsedTicks<<4)&mask    │    Random            │
└─────────┴────────────────────────────┴──────────────────────┘
  Bits:     63-60      59-20                   19-0
```

### ID generation
```csharp
elapsedTicks = currentTime.Ticks - epoch.Ticks
// Validate: elapsedTicks >= 0 && elapsedTicks < (1L << 56)

interval = (elapsedTicks << 4) & 0x0FFFFFFFFFF00000L  // 10 F's for 40 bits
entropy = (randomBytes) & 0x0FFFFF                    // 20 bits
if (entropy > MaxEntropy) entropy -= Headroom         // cap for monotonic safety
id = interval | entropy
```

- `IntervalMask`: `0x0FFFFFFFFFF00000L` (bits 20-59, exactly 10 F's: 40 bits ÷ 4 bits per hex digit).
- `MaxEntropy`: `0xC1BE7` (793,575) = 2²⁰ - 1 - 255,000.
- `Headroom`: 255,000 = 1,000 increments × 255, the largest random increment.

### DateTime extraction
```csharp
reducedInterval = (id >> 20) & 0xFFFFFFFFFF  // bits 20-59
elapsedTicks = reducedInterval << 16         // back to tick scale
dateTime = epoch.AddTicks(elapsedTicks)
```

- One interval is 65,536 ticks (2¹⁶), about 6.5536 ms, so an extracted time is always `<=` the original time and less than 6.6 ms before it.
- The time range is about 228 years from the epoch.

### Monotonicity
Lock-free, using `Interlocked.CompareExchange`:
```csharp
if (newId > lastId) lastId = newId  // fast path
else newId = lastId + randomByte    // slow path: increment by 1-255
```
Overflowing into the next interval is acceptable, not an error. An ID is returned only after a successful compare-and-swap raises `_lastId` to it, so one instance never returns the same ID twice. A zero increment byte just loops and draws again.

Uniqueness is per `IdGenerator` instance, for its lifetime. Separate instances, and a restarted process, can collide. They are time-ordered but not globally monotonic. The fast path keeps fresh entropy only when it beats the last ID, so in a burst every instance climbs to the top of the entropy range and increments through the same band. A simulation of two instances in one interval measured about 1 in 670,000 at 1 ID each, about 1 in 2,100 at 10 each, and about 1 in 5 at 100 each. The readme's **Uniqueness Across Instances** section documents this for users: keep it accurate if generation changes.

## Architecture
- **`IdGenerator`** (sealed): init properties `Epoch`, `TimeProvider` and `EntropyProvider`; methods `NextId()`, `NextString()` and `NextString(template, placeholder)`.
- **`EntropyProvider`** (public, unsealed so tests can subclass it): thread-safe, buffered entropy. `EntropyProvider.Shared` is the default; `new EntropyProvider(bufferSize)` makes a private buffer.
- **Extension members** (C# 14):
  - `extension(IdGenerator)` (static): `ParseRawString`, `TryParseRawString`, `ParseFormattedString` and `TryParseFormattedString`.
  - `extension(IdGenerator self)` (instance): `ExtractDateTime`, `GetMinId`, `GetMaxId`, `GetIdRange` and their string versions.
- **`IdFormatter`** (internal, static): Crockford Base32 encoding and decoding, 12 characters at most. Decoding maps the confusable characters I/i/L/l to 1 and O/o to 0. The raw format is `####-####-####`.

### Init properties and constructors
Init properties are set *after* the constructor runs, so copying one into a field in the constructor captures its default value. Read the properties directly where they are needed.

```csharp
public DateTime Epoch { get; init; } = DateTime.UnixEpoch;
private long Elapsed(DateTime time) => time.Ticks - Epoch.Ticks;  // ✅ reads the initialised value
```

## Tests
- Tests are in `tests/Smidgen.Tests/Smidgen.Tests.csproj`, and use xUnit v3 on the Microsoft Testing Platform. Files are named after the class and area they test, with a `Tests.cs` suffix (for example `IdGenerator.RangeQuery.Tests.cs`).
- Where appropriate, use `[Theory]` over `[Fact]` to test several scenarios with one method when only the inputs and outputs vary.
- Aim for 100% coverage of public methods and types. Measure it with `dotnet test --coverage --coverage-output-format cobertura` (reports land in `TestResults/`, one per target framework plus a merged one).
- The Testing Platform doesn't support VSTest's `--filter` syntax. To run one test class: `dotnet test --project tests/Smidgen.Tests --filter-class "WarpCode.Smidgen.Tests.IdGeneratorRangeQueryTests"` (add `-f net10.0` for a single target framework).

### Deterministic testing
Tests use the fakes in `TestHelpers.cs` rather than `DateTime.UtcNow` or real randomness:
- `FakeTimeProvider`: fixed time from the constructor, `SetTime(DateTime)` and `Increment(TimeSpan)`.
- `FakeEntropyProvider(fixedEntropy)` or `FakeEntropyProvider(byte[])`: `GetByte()` always returns 1, so with an unchanged time `id2 == id1 + 1`.
- `MinimumUniqueByteValues = 200` is the threshold for every entropy-uniqueness assertion.

```csharp
var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
var generator = new IdGenerator
{
    Epoch = DateTime.UnixEpoch,
    TimeProvider = timeProvider,
    EntropyProvider = new FakeEntropyProvider(fixedEntropy: 12345)
};
```

- A monotonic generator never returns the same ID twice, so don't expect repeated `NextId()` calls to match, and account for the increments in round-trip tests.
- Assert exact values, not just `id > 0`.
- For DateTime extraction, compute `(knownTime - extractedTime).TotalMilliseconds` without `Math.Abs`, and assert it is `>= 0` and `< 7`.

## Future goals
- A `CODE_OF_CONDUCT.md`.
- Benchmarks: single-threaded throughput in IDs per second compared with similar libraries.
