# Smidgen - Coding Guidelines

## Project Overview

Smidgen is a high-performance library for generating 60-bit monotonic identifiers stored in `long` (Int64).
IDs combine a 40-bit time component with 20-bit entropy, formatted as human-readable strings using Crockford Base32 encoding.

**Targets**: .NET 8, .NET 10  
**Code Style**: See `.editorconfig`

## Core Principles

- Minimal public API surface for stability
- Thread-safe, lock-free implementation using `Interlocked` operations
- Zero allocations in hot path (stack-allocated buffers, `stackalloc`)
- Aggressive inlining (`[MethodImpl(MethodImplOptions.AggressiveInlining)]`)
- Modern C# 14 features (extension members, pattern matching, spans)
- Comprehensive deterministic tests (100% coverage)

## ID Format (60 bits in 64-bit long)

```
┌─────────┬────────────────────────────┬──────────────────────┐
│ 4 bits  │         40 bits            │       20 bits        │
│ Unused  │    Time (Interval)         │    Entropy           │
│ (0000)  │  (elapsedTicks<<4)&mask    │    Random            │
└─────────┴────────────────────────────┴──────────────────────┘
  Bits:     63-60      59-20                   19-0
```

## Key Formulas

### ID Generation
```csharp
elapsedTicks = currentTime.Ticks - epoch.Ticks
// Validate: elapsedTicks >= 0 && elapsedTicks < (1L << 56)

interval = (elapsedTicks << 4) & 0x0FFFFFFFFFF00000L  // 10 F's for 40 bits
entropy = (randomBytes) & 0x0FFFFF  // 20 bits
if (entropy > MaxEntropy) entropy -= Headroom  // Cap for monotonic safety
id = interval | entropy
```

**Constants**:
- `IntervalMask`: `0x0FFFFFFFFFF00000L` (bits 20-59, exactly 10 F's)
- `MaxEntropy`: `0xC1BE7` (793,575) = 2²⁰ - 1 - 255,000
- `Headroom`: 255,000 = 1,000 increments × 255 max random byte

### DateTime Extraction
```csharp
reducedInterval = (id >> 20) & 0xFFFFFFFFFF  // Extract bits 20-59
elapsedTicks = reducedInterval << 16         // Restore to tick scale
dateTime = epoch.AddTicks(elapsedTicks)      // Precision: ±6.5ms
```

**Precision Loss**: 65,536 ticks (2¹⁶) = 6.5536ms per interval  
**Extraction Rule**: `extractedTime <= originalTime` (always)

### Monotonicity
Lock-free using `Interlocked.CompareExchange`:
```csharp
if (newId > lastId) lastId = newId  // Fast path (lock-free)
else newId = lastId + randomByte    // Slow path (increment by 1-255)
```
Overflow to next interval is acceptable (not an error).

## Architecture

### Public Components
- **`IdGenerator`** (sealed class): Core ID generation
  - Init properties: `Epoch`, `TimeProvider`, `EntropyProvider`
  - Methods: `NextId()`, `NextString()`, `NextString(template, placeholder)`

- **`EntropyProvider`** (public class): Thread-safe entropy
  - Static: `EntropyProvider.Shared` (shared buffer, default)
  - Constructor: `new EntropyProvider(bufferSize)` for custom buffers
  - Virtual methods (subclass in tests for determinism)

- **Extension Methods** (C# 14 syntax):
  - **Static extensions**: `extension(IdGenerator)` for parsing methods
    - `ParseRawString()`, `TryParseRawString()`, `ParseFormattedString()`, `TryParseFormattedString()`
  - **Instance extensions**: `extension(IdGenerator self)` for other methods
    - `ExtractDateTime()`, `GetMinId()`, `GetMaxId()`, `GetIdRange()`

### Internal Components
- **`IdFormatter`**: Static class, Crockford Base32 encoding/decoding (12-char max)
  - Confusable characters: I/i/L/l → 1, O/o → 0
  - Raw format: `####-####-####` (e.g., `"0000-0000-0001"`)

## Critical Patterns

### Init Properties and Constructors
**❌ Wrong**: Copying init properties to fields in constructor captures DEFAULT values, not initialized values.
```csharp
private readonly DateTime _epoch;
public DateTime Epoch { get; init; } = DateTime.UnixEpoch;
public IdGenerator() { _epoch = Epoch; }  // ❌ Gets default!
```

**✅ Correct**: Use properties directly.
```csharp
public DateTime Epoch { get; init; } = DateTime.UnixEpoch;
private void Method() { var elapsed = time.Ticks - Epoch.Ticks; }  // ✅
```

### Bit Manipulation
- **Interval Mask**: 40 bits at positions 20-59 = **10 F's** (not 11!)
  - Formula: 40 bits ÷ 4 bits/hex digit = 10 hex digits
- **Extraction**: `(id >> 20)` then `(<< 16)` to restore tick scale

## Testing Guidelines

### Deterministic Testing
All tests use `FakeTimeProvider` and `FakeEntropyProvider` (no `DateTime.UtcNow`).

```csharp
var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
var entropyProvider = new FakeEntropyProvider(fixedEntropy: 12345);

var generator = new IdGenerator
{
    Epoch = DateTime.UnixEpoch,
    TimeProvider = timeProvider,
    EntropyProvider = entropyProvider
};
```

### Testing Monotonic Generators
- **Cannot** expect same ID from multiple `NextId()` calls (monotonic logic increments)
- **Must** use exact value validation (not just `id > 0`)
- **Must** account for increment behavior in round-trip tests

### DateTime Extraction Tests
- Use `(knownTime - extractedTime).TotalMilliseconds` (no `Math.Abs`)
- Assert `difference >= 0 && difference < 7` (7ms tolerance for ~6.5ms precision)
- `extractedTime <= knownTime` always (due to precision loss)

### Uniqueness Tests
Use constant for consistency (e.g., `MinimumUniqueByteValues = 200` for entropy tests).

### Test Helpers (TestHelpers.cs)
- **FakeTimeProvider**: Set fixed time with `SetTime()`, advance with `Increment(TimeSpan)`
- **FakeEntropyProvider**: 
  - Always returns increment of 1 for deterministic monotonicity tests
  - Can initialize with fixed entropy value or byte array
  - Tests expect `id2 == id1 + 1` when using same time
- **Constants**:
  - `MinimumUniqueByteValues = 200` - Use for all entropy uniqueness assertions

## API Usage

### Basic
```csharp
var generator = IdGenerator.Default;  // or: new IdGenerator()
long id = generator.NextId();
string raw = generator.NextString();  // "XXXX-XXXX-XXXX"
string formatted = generator.NextString("ID-############");
```

### DI Registration
```csharp
services.AddSingleton<IdGenerator>();  // or: IdGenerator.Default
```

### Parsing & Extraction
```csharp
long id = IdGenerator.ParseRawString("0000-0000-0001");  // Static extension
DateTime dt = generator.ExtractDateTime(id);  // Instance extension
```

### Range Queries
```csharp
long minId = generator.GetMinId(startDate);
long maxId = generator.GetMaxId(endDate);
// SQL: WHERE id >= {minId} AND id <= {maxId}
```

## Performance Characteristics

- **Time Range**: 228 years from epoch (40-bit interval)
- **Precision**: ±6.5536ms (65,536 ticks/interval)

- **Concurrency**: Lock-free fast path, minimal contention
- **Memory**: Zero allocations in hot path

## Future goals: 
### Create github actions to build, package and release to Nuget
- Ensure documentation (ReadMe.md) is clear and comprehensive for users.
- Correct artifacts for well maintained open-source project (LICENSE, CONTRIBUTING.md, CODE_OF_CONDUCT.md).
- **CI/CD**: Automate build, test, and release process with GitHub Actions for consistent quality and faster releases.
- **NuGet Packaging**: Create a NuGet package with proper metadata, versioning, and documentation for easy consumption by .NET developers.
- Support semantic versioning and include changelogs in releases.

### Benchmark (not implemented)
- **Throughput**: How many IDs/sec single-threaded can we get compared to other similar libraries?