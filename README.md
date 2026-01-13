# Codezerg.BinarySerialization

[![NuGet](https://img.shields.io/nuget/v/Codezerg.BinarySerialization.svg)](https://www.nuget.org/packages/Codezerg.BinarySerialization/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)

A high-performance .NET binary serialization library featuring **key interning**, **struct templates**, and **streaming support**. Designed for memory efficiency with embedded commands that allow in-stream definitions.

## Features

- **Compact Format** - Single-byte type markers with optimized encoding for small values
- **Key Interning** - Repeated map keys are stored once and referenced by ID (30%+ space savings)
- **Struct Templates** - Define object shapes once, reuse for multiple instances
- **Streaming Support** - Unbounded arrays and maps for streaming scenarios
- **DoS Protection** - Built-in limits for table sizes, string lengths, and nesting depth
- **Zero Dependencies** - Pure .NET with no external dependencies

## Installation

```bash
dotnet add package Codezerg.BinarySerialization
```

## Quick Start

### High-Level API (Recommended)

```csharp
using Codezerg.BinarySerialization;

// Define your class
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

// Serialize
var person = new Person { Name = "Alice", Age = 30, IsActive = true };
byte[] bytes = BinarySerializer.Serialize(person);

// Deserialize
var restored = BinarySerializer.Deserialize<Person>(bytes);
```

### Low-Level API

```csharp
using Codezerg.BinarySerialization;

// Write data
using var stream = new MemoryStream();
using var writer = new BinarySerializationWriter(stream, leaveOpen: true);

writer.WriteMapHeader(2);
writer.Write("name");
writer.Write("Alice");
writer.Write("age");
writer.Write(30);
writer.Flush();

// Read data
stream.Position = 0;
using var reader = new BinarySerializationReader(stream);

var count = reader.ReadMapHeader();
for (int i = 0; i < count; i++)
{
    var key = reader.ReadString();
    var type = reader.PeekType();

    if (type == SerializedType.String)
        Console.WriteLine($"{key}: {reader.ReadString()}");
    else if (type == SerializedType.Integer)
        Console.WriteLine($"{key}: {reader.ReadInt32()}");
}
```

## High-Level Serialization

### Attributes

Control serialization with attributes:

```csharp
public class User
{
    [BinaryProperty("user_name")]  // Custom serialized name
    public string Name { get; set; }

    [BinaryProperty(Order = 1)]    // Control serialization order
    public int Id { get; set; }

    [BinaryIgnore]                  // Exclude from serialization
    public string PasswordHash { get; set; }
}
```

### Supported Types

The high-level API supports:
- Primitives: `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`
- Strings and `byte[]`
- `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`
- Enums
- Nullable types
- Collections: Arrays, `List<T>`, `Dictionary<TKey, TValue>`
- Nested objects

### Options

```csharp
var options = new SerializerOptions
{
    UseKeyInterning = true,   // Enable key interning (default: true)
    IncludeFields = false     // Include public fields (default: false)
};

var bytes = BinarySerializer.Serialize(obj, options);
```

## Key Interning

Reduce payload size by interning repeated map keys:

```csharp
using var writer = new BinarySerializationWriter(stream);

writer.WriteArrayHeader(1000);
for (int i = 0; i < 1000; i++)
{
    writer.WriteMapHeader(3);
    writer.WriteKey("username");  // Stored once, referenced 999 times
    writer.Write($"user{i}");
    writer.WriteKey("email");     // Same - huge space savings!
    writer.Write($"user{i}@example.com");
    writer.WriteKey("active");
    writer.Write(i % 2 == 0);
}
```

**Result:** ~32% smaller payload compared to writing keys as full strings every time.

## Struct Templates

For highly repetitive object structures, define a template once:

```csharp
using var writer = new BinarySerializationWriter(stream);

// Define struct template (keys stored once)
var personStruct = writer.DefineStruct("name", "age", "city");

// Write instances - only values are serialized!
writer.UseStruct(personStruct);
writer.Write("Alice");
writer.Write(30);
writer.Write("New York");

writer.UseStruct(personStruct);
writer.Write("Bob");
writer.Write(25);
writer.Write("London");
```

## Streaming Collections

Use unbounded collections when you don't know the size upfront:

```csharp
using var writer = new BinarySerializationWriter(stream);

writer.BeginArray();  // Start unbounded array

// Write items as they arrive
foreach (var item in GetStreamingData())
{
    writer.Write(item);
}

writer.WriteEnd();  // End the array
```

Reading unbounded collections:

```csharp
using var reader = new BinarySerializationReader(stream);

var length = reader.ReadArrayHeader();  // Returns -1 for unbounded

while (!reader.IsEnd())
{
    var value = reader.ReadString();
    Console.WriteLine(value);
}
reader.ReadEnd();
```

## Supported Types

| Type | Methods |
|------|---------|
| Null | `WriteNil()` |
| Boolean | `Write(bool)`, `ReadBoolean()` |
| Integers | `Write(sbyte/byte/short/ushort/int/uint/long/ulong)`, `ReadInt32()`, `ReadInt64()`, `ReadUInt64()` |
| Floats | `Write(float/double)`, `ReadSingle()`, `ReadDouble()` |
| Strings | `Write(string)`, `ReadString()` |
| Binary | `Write(byte[])`, `Write(ReadOnlySpan<byte>)`, `ReadBinary()` |
| Arrays | `WriteArrayHeader(count)`, `BeginArray()`, `ReadArrayHeader()` |
| Maps | `WriteMapHeader(count)`, `BeginMap()`, `ReadMapHeader()` |

## Security

Configure reader limits to prevent DoS attacks:

```csharp
var limits = new ReaderLimits
{
    MaxStringLength = 1_000_000,      // 1MB max string
    MaxBinaryLength = 10_000_000,     // 10MB max binary
    MaxKeyTableSize = 1000,           // Max interned keys
    MaxStructTableSize = 100,         // Max struct templates
    MaxDepth = 50                     // Max nesting depth
};

using var reader = new BinarySerializationReader(stream, limits: limits);
```

## Binary Format

The format uses a single-byte type marker system:

| Range | Description |
|-------|-------------|
| `0x00-0x7F` | Positive fixint (0-127) |
| `0x80-0x8F` | Fixmap (0-15 pairs) |
| `0x90-0x9F` | Fixarray (0-15 elements) |
| `0xA0-0xBF` | Fixstr (0-31 bytes) |
| `0xC0-0xD6` | Extended types (nil, bool, numbers, strings, collections) |
| `0xE0-0xEF` | Negative fixint (-16 to -1) |
| `0xF0-0xFF` | Commands (SET_KEY, USE_KEY, DEFINE_STRUCT, etc.) |

See [docs/serialization_format_specification.md](docs/serialization_format_specification.md) for the complete specification.

## Building

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run examples
dotnet run --project examples/Codezerg.BinarySerialization.Examples

# Create NuGet package
dotnet pack -c Release
```

## License

MIT License - see [LICENSE](LICENSE) for details.
