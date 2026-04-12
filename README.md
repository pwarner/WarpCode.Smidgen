# Smidgen

[![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/pwarner/WarpCode.Smidgen)](LICENSE)

**Smidgen** is a high-performance .NET library for generating **60-bit monotonic identifiers** stored as `long` (Int64). 

These IDs combine a **40-bit time component** with **20-bit cryptographic entropy**, providing time-ordered identifiers with extremely low collision probability. 
Ideal for databases, and any applications where coordination-free ID generation within a process is valuable.

## ✨ Features

- **🚀 High Performance**: Zero allocations in hot path, lock-free atomic operations
- **📅 Time-Ordered**: IDs are sortable by creation time with ~6.5ms precision (globally)
- **🔒 Thread-Safe**: Concurrent ID generation without locks using `Interlocked` operations
- **🎯 Monotonic**: Guaranteed increasing IDs **within a single instance**, even with clock skew or concurrent access
- **📝 Human-Readable**: [Crockford Base32 encoding](https://www.crockford.com/base32.html) (e.g., `0HMK-QRST-1234`)
- **🎲 Cryptographic Entropy**: Uses `RandomNumberGenerator` for unpredictability
- **⚡ Compact**: 60 bits fit in a standard `long` (Int64) - database friendly
- **🔍 Range Queries**: Extract timestamps and generate ID ranges for efficient queries
- **🧪 Testable**: Designed with dependency injection and deterministic testing in mind

## 📦 Installation

```bash
# Coming soon to NuGet
dotnet add package WarpCode.Smidgen
```

For now, clone and build from source:

```bash
git clone https://github.com/pwarner/WarpCode.Smidgen.git
cd WarpCode.Smidgen
dotnet build
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
- **Entropy Component (20 bits)**: Cryptographically secure random value (~1 million values per interval)
- **Monotonic Guarantee**: Within an instance, IDs always increase even with same timestamp

## 🔒 Thread Safety

Smidgen uses lock-free atomic operations (`Interlocked.CompareExchange`) for thread-safe ID generation.

Multiple threads can safely call `NextId()` and its sibling `NextString()` methods concurrently without coordination.

## 📊 Performance Characteristics

- **Zero Allocations**: Stack-allocated buffers (`stackalloc`)
- **Aggressive Inlining**: Hot path methods use `[MethodImpl(MethodImplOptions.AggressiveInlining)]`

## 🧪 Testing

Smidgen is designed for deterministic testing with 100% code coverage:

```csharp
var timeProvider = new FakeTimeProvider(
    new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
var entropyProvider = new FakeEntropyProvider(fixedEntropy: 12345);

var generator = new IdGenerator
{
    Epoch = DateTime.UnixEpoch,
    TimeProvider = timeProvider,
    EntropyProvider = entropyProvider
};

long id1 = generator.NextId();
timeProvider.Advance(TimeSpan.FromMilliseconds(10));
long id2 = generator.NextId();

Assert.True(id2 > id1); // Monotonic guarantee
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
- Per-instance monotonic guarantees
- Extremely low collision probability across instances (20 bits entropy = ~1M values per 6.5ms)

Similar to [Snowflake IDs](https://en.wikipedia.org/wiki/Snowflake_ID) but optimized for .NET with:
- **No coordination required** (no machine/datacenter IDs - simpler deployment)
- Higher entropy (20 bits vs 12) for collision resistance
- Built-in string formatting
- Range query support

**Trade-off**: Unlike Snowflake, Smidgen doesn't guarantee global uniqueness or global monotonicity across instances—it relies on probabilistic uniqueness via cryptographic entropy and time-ordering. 

**Important for distributed systems**: In environments like Kubernetes with multiple pods, each pod's `IdGenerator` instance operates independently:
- ✅ **Time-ordered**: IDs from different instances can still be sorted by approximate creation time (~6.5ms precision)
- ❌ **NOT monotonic globally**: Pod A at time T+1ms might generate a smaller ID than Pod B at time T due to entropy
- ❌ **NOT guaranteed unique**: Collision probability is negligible but theoretically possible

If you need absolute guarantees, consider:
- Adding instance/pod identifiers to your data model
- Using a centralized ID generation service
- Using a coordination-based approach (e.g., database sequences, distributed locks)

## 📚 Additional Resources

- [Detailed Documentation](.github/copilot-instructions.md) - Implementation details and algorithms
- [API Reference](src/Smidgen/IdGenerator.cs) - Full XML documentation
- [Test Suite](tests/Smidgen.Tests/) - Comprehensive test examples

## 🤝 Contributing

Contributions are welcome! This project follows standard open-source practices:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📋 Requirements

- **.NET 8** or **.NET 10** (or later)
- No external dependencies (uses BCL only)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 💬 Feedback & Support

- **Issues**: [GitHub Issues](https://github.com/pwarner/WarpCode.Smidgen/issues)
- **Discussions**: [GitHub Discussions](https://github.com/pwarner/WarpCode.Smidgen/discussions)

---

Made with ❤️ by Pete Warner
