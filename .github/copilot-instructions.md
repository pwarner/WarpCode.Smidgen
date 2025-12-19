# Smidgen - Architecture & Design Vision

## Project Overview

Smidgen is a high-performance library for generating compact, sortable, human-readable identifiers using Crockford Base32 encoding.
The library is designed for .NET 9 with C# 13, emphasizing performance, safety, and a minimal public API surface.
However, we will want to target .NET 8 as the minimum supported framework version to reach a wider audience.
Potentially we could multi-target .NET 8 and .NET 10 if there are any APIs that are only available in .NET 10 that we want to use.
In all cases, we will use the latest C# language features available in C# 13 since the build agent will support the .NET 10 SDK.

## Core Components

### 1. **IdGenerator** (Public)
Generates 128-bit unsigned integer identifiers that combine:
- **Time component** (configurable bits) - time value with configurable precision
- **Entropy component** (configurable bits) - random value
- **Monotonic guarantee** - ensures IDs are always increasing, even in concurrent scenarios
- **Flexible configuration** - via GeneratorSettings for different use cases

### 2. **GeneratorSettings** (Public)
Configures IdGenerator behavior:
- Specifies time and entropy bit allocations
- Provides functions for time, entropy, and increment generation
- Includes preset configurations: SmallId (64-bit), Id80, Id96, BigId (128-bit)

### 3. **IdFormatter** (Public)
Converts 128-bit identifiers to/from human-readable formatted strings:
- Uses customizable format templates with placeholder characters (default: `#`)
- Example: `"PRE-####-####-SUF"` ? `"PRE-ABCD-1234-SUF"`
- Supports up to 26 placeholders (130 bits > 128 bits of UInt128)

### 4. **CrockfordEncoding** (Internal)
Low-level Base32 encoding/decoding optimized for UInt128 values:
- Encodes UInt128 values to Crockford Base32 ASCII bytes
- Decodes Base32 bytes back to UInt128
- Handles confusable characters (O?0, I?1, L?1)
- Case-insensitive decoding

### 5. **TimeElements** (Internal)
Helper functions for time element generation:
- Provides millisecond, microsecond, and tick precision
- Converts time values back to DateTime

### 6. **EntropyElements** (Internal)
Thread-safe cryptographic entropy provider:
- Provides entropy values of varying widths (16, 32, 40, 48, 64 bits)
- Uses shared buffer with lock-free reads and locked refills
- All values have top bit cleared (reserved as carry bit)

## Code Organization

### Source Structure
```
src/Smidgen/
    IdGenerator.cs       (Public) - Monotonic ID generation with GeneratorSettings
    GeneratorSettings.cs (Public) - Configuration for ID generation
    IdFormatter.cs       (Public) - Format/Parse with templates
    CrockfordEncoding.cs (Internal) - Low-level UInt128 encoding/decoding
    TimeElements.cs      (Internal) - Time element generation helpers
    EntropyElements.cs   (Internal) - Cryptographic entropy provider
```

### Test Structure
```
tests/Smidgen.Tests/
    IdGeneratorTests.cs
    GeneratorSettingsTests.cs
    IdFormatterTests.cs
    CrockfordEncodingTests.cs
    TimeElementsTests.cs
    EntropyElementsTests.cs
