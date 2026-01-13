# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Codezerg.BinarySerialization is a high-performance .NET 8.0 binary serialization library featuring key interning, struct templates, and streaming support. The format is designed for memory efficiency with embedded commands that allow in-stream definitions.

## Project Structure

```
Codezerg.BinarySerialization/
├── src/Codezerg.BinarySerialization/       # Main library
│   ├── TypeMarkers.cs                      # Binary format constants
│   ├── BinarySerializationWriter.cs        # Low-level serialization
│   ├── BinarySerializationReader.cs        # Low-level deserialization
│   ├── BinarySerializer.cs                 # High-level reflection-based API
│   └── Attributes.cs                       # Serialization attributes
├── tests/Codezerg.BinarySerialization.Tests/
│   ├── BinarySerializationTests.cs         # Low-level API tests
│   ├── BinarySerializerTests.cs            # High-level API tests
│   ├── ResiliencyTests.cs                  # Schema evolution tests
│   └── NestedTypeTests.cs                  # Nested type tests
├── examples/Codezerg.BinarySerialization.Examples/
│   └── Program.cs                          # Usage examples
└── docs/
    └── serialization_format_specification.md
```

## Build Commands

```bash
# Build the solution
dotnet build

# Run all tests (104 tests)
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~BinarySerializerTests"

# Run examples
dotnet run --project examples/Codezerg.BinarySerialization.Examples

# Build in release mode
dotnet build -c Release

# Create NuGet package
dotnet pack -c Release
```

## API Overview

### High-Level API (Recommended)

```csharp
// Serialize/deserialize objects automatically
var bytes = BinarySerializer.Serialize(myObject);
var obj = BinarySerializer.Deserialize<MyClass>(bytes);

// With options
var options = new SerializerOptions { UseKeyInterning = true };
BinarySerializer.Serialize(stream, obj, options);
```

### Low-Level API

```csharp
// Manual control over serialization
using var writer = new BinarySerializationWriter(stream);
writer.WriteMapHeader(2);
writer.WriteKey("name");  // Key interning
writer.Write("Alice");
writer.WriteKey("age");
writer.Write(30);

// Reading
using var reader = new BinarySerializationReader(stream);
var count = reader.ReadMapHeader();
var key = reader.ReadKey();
var value = reader.ReadString();
```

### Attributes

```csharp
public class User
{
    [BinaryProperty("user_name")]  // Custom name
    public string Name { get; set; }

    [BinaryProperty(Order = 1)]    // Serialization order
    public int Id { get; set; }

    [BinaryIgnore]                  // Exclude from serialization
    public string CachedData { get; set; }
}
```

## Supported Types

- **Primitives**: bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal
- **Strings**: string (UTF-8 encoded)
- **Binary**: byte[]
- **Special**: DateTime, DateTimeOffset, TimeSpan, Guid
- **Enums**: All enum types (serialized as long)
- **Nullable**: Nullable<T> for all supported types
- **Collections**: Arrays, List<T>, Dictionary<TKey, TValue>
- **ADO.NET**: DataTable, DataSet, IDataReader (serialize only, deserialize as DataTable)
- **Complex**: Classes and structs (including nested types)

## Architecture

### Binary Format (Version 1.2.0)

The serialization format uses a single-byte type marker system:
- `0x00-0x7F`: Positive fixint (values 0-127 encoded directly)
- `0x80-0x8F`: Fixmap (0-15 key-value pairs)
- `0x90-0x9F`: Fixarray (0-15 elements)
- `0xA0-0xBF`: Fixstr (0-31 byte strings)
- `0xC0-0xD6`: Extended types (nil, bool, bin, float, int, str, array, map)
- `0xE0-0xEF`: Negative fixint (-16 to -1)
- `0xF0-0xFF`: Commands (key interning, struct templates, streaming)

### Command System

Commands enable space-efficient serialization through in-stream definitions:
- **SET_KEY (0xF0)** / **USE_KEY (0xF1)**: Key interning for repeated map keys
- **DEFINE_STRUCT (0xF2)** / **USE_STRUCT (0xF3)**: Struct templates for repeated object shapes
- **CLEAR_KEYS (0xF4)** / **CLEAR_STRUCTS (0xF5)** / **CLEAR_ALL (0xF6)**: Table management
- **BEGIN_ARRAY (0xF7)** / **BEGIN_MAP (0xF9)** / **END (0xF8)**: Unbounded collections

### Data Encoding

- Multi-byte integers: Big-endian (network byte order)
- Floats: IEEE 754, big-endian
- Strings: UTF-8 with byte length prefix
- Varints: 1-4 bytes depending on value magnitude

### DataTable/DataSet/IDataReader Format

- **DataTable**: Serialized as array of row dictionaries `[{col: val, ...}, ...]`
- **DataSet**: Serialized as array of DataTables `[DataTable, ...]`
- **IDataReader**: Same format as DataTable, uses streaming array (row count unknown)
- Column names use key interning for efficiency
- Supports `List<>` and `Dictionary<,>` as column values
- `DBNull.Value` serialized as nil (0xC0)

## Key Features

### Schema Evolution / Resiliency

The deserializer handles schema mismatches gracefully:
- Extra properties in data are skipped
- Missing properties keep default values
- Type mismatches are skipped (property keeps default)
- Works with nested objects and collections

### Security

Configure reader limits to prevent DoS attacks:

```csharp
var limits = new ReaderLimits
{
    MaxStringLength = 1_000_000,
    MaxBinaryLength = 10_000_000,
    MaxKeyTableSize = 1000,
    MaxStructTableSize = 100,
    MaxDepth = 50
};
using var reader = new BinarySerializationReader(stream, limits: limits);
```

## Documentation

- README.md: Usage examples and API reference
- docs/serialization_format_specification.md: Full binary format specification
