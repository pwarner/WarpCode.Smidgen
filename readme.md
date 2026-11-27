# Smidgen

[![.NET](https://img.shields.io/badge/.NET-10%20%7C%2011-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/pwarner/WarpCode.Smidgen)](LICENSE)

**Smidgen** is a high-performance .NET library for generating **60-bit monotonic identifiers** stored as `long` (Int64). 

These IDs combine a **40-bit time component** with **20-bit cryptographic entropy**, providing time-ordered identifiers that never repeat within a generator instance. 
Ideal for databases, and any applications where coordination-free ID generation within a process is valuable.

## ✨ Features

- **🚀 High Performance**: Zero allocations in hot path, lock-free atomic operations
- **📅 Time-Ordered**: IDs are sortable by creation time with ~6.5ms precision (globally)
- **🔒 Thread-Safe**: Concurrent ID generation without locks using `Interlocked` operations
- **🎯 Monotonic**: Guaranteed increasing IDs **within a single instance**, even with clock skew or concurrent access
- **📝 Human-Readable**: [Crockford Base32 encoding](https://www.crockford.com/base32.html) (e.g., `0HMK-QRST-1234`)
- **🎲 Cryptographic Entropy**: Uses `RandomNumberGenerator` for the random starting point of each interval
- **⚡ Compact**: 60 bits fit in a standard `long` (Int64) - database friendly
- **🔍 Range Queries**: Extract timestamps and generate ID ranges for efficient queries
- **🧪 Testable**: Designed with dependency injection and deterministic testing in mind

## 📦 Installation

```bash
dotnet add package WarpCode.Smidgen
```

## 🎯 Quick Start

### Basic Usage

```csharp
using WarpCode.Smidgen;

// Create a generator with default settings
var generator = new IdGenerator();

// Generate a numeric ID
long id = generator.NextId();
// Example: 3458764513820540928

// Generate a formatted string
string rawId = generator.NextString();
// Example: "0HMK-QRST-1234"

// Generate with custom template
string customId = generator.NextString("ORDER-######-######");
// Example: "ORDER-0HMKQR-ST1234"
```

### Custom Configuration
Time starts from the Unix epoch by default, but you can configure a custom epoch:

```csharp
using WarpCode.Smidgen;

var generator = new IdGenerator
{
    Epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
};

long id = generator.NextId();
```

### Dependency Injection

```csharp
// Startup.cs or Program.cs
services.AddSingleton<IdGenerator>();

// Or register a configured instance
services.AddSingleton(new IdGenerator
{
    Epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
});
```

## 💡 Common Use Cases

### Parsing and Extracting Timestamps

```csharp
// Parse raw formatted string
long id = IdGenerator.ParseRawString("0HMK-QRST-1234");

// Parse custom formatted string
long id2 = IdGenerator.ParseFormattedString("ORDER-0HMK-QRST-1234", "ORDER-############");

// Extract creation timestamp (±6.5ms precision)
DateTime createdAt = generator.ExtractDateTime(id);
```

### Database Range Queries

```csharp
var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var endDate = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

long minId = generator.GetMinId(startDate);
long maxId = generator.GetMaxId(endDate);

// or in one call
(minId, maxId) = generator.GetIdRange(startDate, endDate);

// SQL: SELECT * FROM orders WHERE id >= @minId AND id <= @maxId
```

## 🏗️ ID Structure

Smidgen IDs use 60 bits of a 64-bit `long`:

```
┌─────────┬────────────────────────────┬──────────────────────┐
│ 4 bits  │         40 bits            │       20 bits        │
│ Unused  │    Time (Interval)         │    Entropy           │
│ (0000)  │  (elapsedTicks<<4)&mask    │    Random            │
└─────────┴────────────────────────────┴──────────────────────┘
  Bits:     63-60      59-20                   19-0
```

- **Time Component (40 bits)**: Timespan of 83,400 days from the configured epoch (~228.33 years), to ~6.5ms precision
- **Entropy Component (20 bits)**: Cryptographically secure random value. The starting value in each interval is capped at 793,575, leaving headroom for increments
- **Monotonic Guarantee**: Within an instance, IDs always increase and never repeat, even with the same timestamp. Separate instances can collide: see **Uniqueness Across Instances** below
- **Not a Secret**: Within an interval, consecutive IDs from one instance differ by only 1-255, so don't use an ID as an unguessable token

## 🔒 Thread Safety

Smidgen uses lock-free atomic operations (`Interlocked.CompareExchange`) for thread-safe ID generation.

Multiple threads can safely call `NextId()` and its sibling `NextString()` methods concurrently without coordination.

## 📊 Performance Characteristics

- **Zero Allocations**: Stack-allocated buffers (`stackalloc`)
- **Aggressive Inlining**: Hot path methods use `[MethodImpl(MethodImplOptions.AggressiveInlining)]`

## 🧪 Testing

Time and entropy are injectable, so code that generates IDs can be tested deterministically.
`IdGenerator` takes any `TimeProvider`, such as `FakeTimeProvider` from the
[Microsoft.Extensions.TimeProvider.Testing](https://www.nuget.org/packages/Microsoft.Extensions.TimeProvider.Testing) package,
and `EntropyProvider`'s methods are virtual, so a subclass can supply fixed entropy:

```csharp
using Microsoft.Extensions.Time.Testing;
using WarpCode.Smidgen;

var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));

var generator = new IdGenerator
{
    TimeProvider = timeProvider,
    EntropyProvider = new FixedEntropyProvider()
};

long id1 = generator.NextId();
long id2 = generator.NextId();             // same instant: id1 + 1
timeProvider.Advance(TimeSpan.FromMilliseconds(10));
long id3 = generator.NextId();             // a later interval, so id3 > id2

// Always returns the same entropy, and increments monotonic IDs by 1
sealed class FixedEntropyProvider() : EntropyProvider(bufferSize: 0)
{
    public override void GetBytes(Span<byte> buffer) => buffer.Clear();
    public override byte GetByte() => 1;
}
```

## 🎓 Why Smidgen?

**Problem**: Simple auto-increment IDs don't work well in distributed systems and lack security, where UUIDs/GUIDs are:
- Often too large (128 bits) for the use case
- Not time-ordered (inefficient for indexing and sorting)
- Not human-friendly

**Solution**: Smidgen provides:
- Compact 60-bit IDs (fits in `long`)
- Time-ordered for natural sorting
- Human-readable Crockford Base32 encoding
- Database-friendly for primary keys and indexes
- Unique, strictly increasing IDs from each generator instance

Similar to [Snowflake IDs](https://en.wikipedia.org/wiki/Snowflake_ID) but optimized for .NET with:
- **No coordination required** (no machine/datacenter IDs - simpler deployment)
- Built-in string formatting
- Range query support

**Trade-off**: Snowflake IDs are unique across machines because each machine has its own ID. Smidgen has no machine ID, so it guarantees uniqueness only within one `IdGenerator` instance.

## ⚠️ Uniqueness Across Instances

**One instance never repeats an ID.** Every ID it returns is strictly greater than the one before, even under concurrent calls or when the clock goes backwards.

**Separate instances can produce the same ID.** That includes several pods or processes, several `IdGenerator` objects in one process, and a process that restarts before its clock has passed the last ID it issued. Two instances can collide only when they generate IDs in the same ~6.5 ms interval, and the chance grows with how many IDs each makes in that interval:

| IDs per instance per interval | Roughly | Chance that two instances share an ID in that interval |
|---|---|---|
| 1 | 150 per second | about 1 in 700,000 |
| 10 | 1,500 per second | about 1 in 7,000 |
| 100 | 15,000 per second | about 1 in 90 |
| 1,000 | 150,000 per second | about 1 in 4 |

Each instance starts each interval at a random point in its ~794,000 entropy values, then adds 1-255 for each further ID in that interval. Two instances collide only if their runs overlap and land on the same value. That is the birthday bound for 20 random bits, the lowest rate any 20-bit scheme can reach.

As a rough guide, two instances each making 1,500 IDs per second all the time would share an ID about once a minute. More instances raise the rate: it grows with the number of pairs. At a few IDs per second, a collision is very unlikely, but still possible.

IDs from separate instances are still **time-ordered** to within ~6.5 ms. They are **not globally monotonic**: within one interval, instance A can produce a smaller ID than instance B produced a moment earlier.

If you need IDs that are unique across instances:
- Use one `IdGenerator` (for example a DI singleton) for everything that shares a key space.
- Enforce uniqueness where it matters, such as a primary key or unique index, and retry on a duplicate.
- Make the key include an instance or pod identifier.
- Use a coordinated source, such as a database sequence or a central ID service.

## 📚 Additional Resources

- [Design Notes](AGENTS.md) - ID layout, algorithms and testing conventions
- [API Reference](src/Smidgen/IdGenerator.cs) - Full XML documentation
- [Test Suite](tests/Smidgen.Tests/) - Comprehensive test examples

## 🤝 Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for how to build, test and submit changes, and [CHANGELOG.md](CHANGELOG.md) for what changed in each release.

## 📋 Requirements

- **.NET 10** or **.NET 11** (or later)
- No external dependencies (uses BCL only)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 💬 Feedback & Support

- **Issues**: [GitHub Issues](https://github.com/pwarner/WarpCode.Smidgen/issues)
- **Discussions**: [GitHub Discussions](https://github.com/pwarner/WarpCode.Smidgen/discussions)

---

Made with ❤️ by Pete Warner
