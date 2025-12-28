# Smidgen - Architecture & Design Vision

## Project Overview

Smidgen is a high-performance library for generating monotonic identifiers of flexible composition.
It supports formatting as human-readable identifiers using Crockford Base32 encoding.
The library is designed to target .NET 8 and .NET 10, emphasizing performance, safety, and a minimal public API surface.

## Core Components

### 1. **IdGenerator** (Public)
Generates 128-bit unsigned integer identifiers that combine:
- **Time component** (configurable bits) - time value with configurable precision
- **Entropy component** (configurable bits) - random value
- **Monotonic guarantee** - ensures IDs are always increasing, even in concurrent scenarios
- **Flexible configuration** - via GeneratorOptions (public) for different use cases
- **Extension methods** - DateTime extraction, parsing, and range queries
- **Provider-based design** - Uses TimeProvider for time operations and internal EntropyProvider for randomness

### 2. **GeneratorOptions** (public)
Configures IdGenerator behavior via fluent builder pattern:
- `UsePreset()` - Apply preset configurations (SmallId, Id80, Id96, BigId)
- `WithTimeAccuracy()` - Configure time precision (Seconds, Milliseconds, Microseconds, Ticks)
- `WithEntropySize()` - Configure entropy bits (16, 24, 32, 40, 48, 56, 64)
- `Since()` - Custom epoch (start date)
- `Until()` - Custom end date for time range

### 3. **IdFormatter** (Internal)
Static class for converting identifiers to/from formatted strings:
- `Format()` - Encodes UInt128 to formatted string with template
- `Parse()` - Decodes formatted string back to UInt128
- `TryParse()` - Safe parsing variant
- Uses Crockford Base32 encoding
- Supports customizable format templates with placeholder characters

### 4. **CrockfordEncoding** (Internal)
Low-level Base32 encoding/decoding optimized for UInt128 values:
- Encodes UInt128 values to Crockford Base32 ASCII bytes
- Decodes Base32 bytes back to UInt128
- Handles confusable characters (O?0, I?1, L?1)
- Case-insensitive decoding

### 5. **EntropyProvider** (Internal)
Thread-safe cryptographic entropy provider:
- Provides entropy values of varying widths (16, 24, 32, 40, 48, 56, 64 bits)
- Uses shared buffer with lock-free reads and locked refills
- All values have top bit cleared (reserved as carry bit)
- Optimized pooling for high-performance scenarios
- **Static Default instance** - Shared instance used by IdGenerator
- **Virtual methods** - Can be subclassed in tests for deterministic behavior

## Code Organization

### Source Structure
```
src/Smidgen/
    IdGenerator.cs                       (Public) - Core ID generation
    IdGenerator.ParsingExtensions.cs     (Public) - Parse/TryParse methods
    IdGenerator.DateTimeExtensions.cs    (Public) - DateTime extraction
    IdGenerator.RangeQueryExtensions.cs  (Public) - Min/Max ID for queries
    GeneratorOptions.cs                  (Public) - Fluent configuration
    GeneratorPreset.cs                   (Public) - Preset enum
    TimeAccuracy.cs                      (Public) - Time precision enum
    EntropySize.cs                       (Public) - Entropy size enum
    IdFormatter.cs                       (Internal) - Static formatting class
    CrockfordEncoding.cs                 (Internal) - Base32 encoding
    EntropyProvider.cs                   (Internal) - Entropy provider
```

### Test Structure
Tests are organized by concern rather than by component:

```
tests/Smidgen.Tests/
    IdGenerator.Generation.Tests.cs      - Core generation (deterministic & non-deterministic)
    IdGenerator.Formatting.Tests.cs      - Formatting and parsing
    IdGenerator.DateTime.Tests.cs        - DateTime extraction
    IdGenerator.RangeQuery.Tests.cs      - Min/Max ID generation
    IdGenerator.Concurrency.Tests.cs     - Thread safety
    IdGenerator.EdgeCases.Tests.cs       - Edge cases and error handling
    CrockfordEncodingTests.cs            - Encoding edge cases
    EntropyProviderTests.cs              - Pooling logic tests
    TestHelpers.cs                       - Shared test fakes (FakeTimeProvider, FakeEntropyProvider)
```

**Total: 260 tests covering all scenarios**

## API Examples

### Basic Usage
```csharp
// Use default configuration (SmallId preset)
var generator = new IdGenerator();

// Generate IDs
var id = generator.NextUInt128();
var rawString = generator.NextRawStringId();
var formatted = generator.NextFormattedId("ID-#############");
```

### Custom Configuration
```csharp
var generator = new IdGenerator(options => options
    .UsePreset(GeneratorPreset.Id96)
    .WithTimeAccuracy(TimeAccuracy.Milliseconds)
    .WithEntropySize(EntropySize.Bits32)
    .Since(new DateTime(2020, 1, 1))
    .Until(new DateTime(2100, 1, 1)));
```

### Extension Methods
```csharp
// DateTime extraction
var dateTime = generator.ExtractDateTime(id);
var dateTime2 = generator.ExtractDateTime(rawStringId);
var dateTime3 = generator.ExtractDateTime(formattedId, template);

// Safe parsing
if (IdGenerator.TryParseRawStringId(input, out var parsed)) // Parsing extensions are static methods
{
    // Use parsed ID
}

// Range queries for database filtering
var (minId, maxId) = generator.GetMinMaxId(startDate, endDate);
// Use minId and maxId in WHERE clauses: id >= minId AND id <= maxId
```

## Testing Philosophy

### Test Organization
Tests are organized by concern rather than by component, following the structure:
- **Generation Tests**: Core ID generation, including deterministic tests using internal constructor
- **Formatting Tests**: String formatting and parsing
- **DateTime Tests**: DateTime extraction from various formats
- **Range Query Tests**: Min/Max ID generation for database queries
- **Concurrency Tests**: Thread safety and parallel generation

### Deterministic Testing
The internal constructor allows deterministic testing using custom providers:
```csharp
// Create fake providers for testing (from TestHelpers.cs)
var timeProvider = new FakeTimeProvider(fixedDateTime);
var entropyProvider = new FakeEntropyProvider(fixedEntropy);

var generator = new IdGenerator(
    options => options.WithTimeAccuracy(TimeAccuracy.Milliseconds).WithEntropySize(EntropySize.Bits16),
    timeProvider,
    entropyProvider);
```

This enables testing scenarios like:
- Backwards time (clock drift)
- Fixed entropy values
- Specific bit layouts
- Monotonicity guarantees

### Provider Pattern
The library uses .NET's TimeProvider pattern for time operations and an internal EntropyProvider for randomness:
- **TimeProvider.System** - Default production time provider
- **EntropyProvider.Default** - Internal default entropy provider (static singleton)
- **Custom Providers** - Subclass providers in tests for deterministic behavior

### Public API Testing
Most tests use the public API to ensure the library works as consumers would use it:
- Configuration via fluent options
- ID generation in various formats
- Parsing and DateTime extraction
- Concurrent access patterns

## Architecture Decisions

### C# 14 Extension Members
Extension methods use modern C# 14 syntax for cleaner, more maintainable code:
```csharp
public static class IdGeneratorParsingExtensions
{
    extension(IdGenerator self)
    {
        public UInt128 ParseRawStringId(string rawStringId)
        {
            // Implementation
        }
    }
}
```

### Provider Pattern
- **TimeProvider**: Uses .NET's built-in TimeProvider abstraction for testability
- **EntropyProvider**: Internal provider following similar pattern
  - Internal class with virtual methods for testing
  - Static `Default` singleton for production use
  - Can be subclassed in test assembly for deterministic testing

### Separation of Concerns
- **GeneratorOptions**: Data carrier only, validates its own invariants
- **IdGenerator**: Core generation logic, all time calculation
- **Extension Classes**: Additional functionality (parsing, DateTime, queries)
- **EntropyProvider**: Specialized random generation with pooling

### Performance Optimizations
- Lock-free atomic operations for concurrent ID generation
- Stack-allocated buffers for encoding (`stackalloc`)
- Aggressive method inlining (`[MethodImpl(MethodImplOptions.AggressiveInlining)]`)
- Pooled entropy buffer to reduce crypto RNG calls

## Notes

- All code follows .NET 8 / .NET 10 conventions
- Uses modern C# features (pattern matching, span-based APIs, extension members)
- Thread-safe implementation with lock-free operations
- Performance characteristics optimized (stack allocation, inlined methods)
- Comprehensive tests covering all scenarios
- Minimal public API surface for stability
- Ready for production use
