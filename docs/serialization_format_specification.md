# Binary Serialization Format Specification

**Version**: 1.2.0

## Overview

This is a binary serialization format designed for memory efficiency with built-in support for **embedded commands**. The key feature is the ability to define keys and struct templates that can be referenced later, eliminating redundant map key strings.

## Design Goals

1. **Memory Efficient**: Minimize payload size through key interning and struct templates
2. **Streaming Compatible**: Can be decoded in a single pass
3. **Self-Describing**: Commands are embedded in the stream, no external schema required
4. **Fast**: Simple decoding logic with minimal branching

## Type System

### Type Markers (1 byte)

```
┌─────────────────┬──────────────────────────────────────────────────┐
│ Range           │ Description                                      │
├─────────────────┼──────────────────────────────────────────────────┤
│ 0x00 - 0x7F     │ Positive fixint (0-127)                          │
│ 0x80 - 0x8F     │ Fixmap (0-15 pairs)                              │
│ 0x90 - 0x9F     │ Fixarray (0-15 elements)                         │
│ 0xA0 - 0xBF     │ Fixstr (0-31 bytes)                              │
├─────────────────┼──────────────────────────────────────────────────┤
│ 0xC0            │ nil                                              │
│ 0xC1            │ false                                            │
│ 0xC2            │ true                                             │
│ 0xC3            │ bin8  (length: uint8)                            │
│ 0xC4            │ bin16 (length: uint16)                           │
│ 0xC5            │ bin32 (length: uint32)                           │
│ 0xC6            │ float32                                          │
│ 0xC7            │ float64                                          │
│ 0xC8            │ uint8                                            │
│ 0xC9            │ uint16                                           │
│ 0xCA            │ uint32                                           │
│ 0xCB            │ uint64                                           │
│ 0xCC            │ int8                                             │
│ 0xCD            │ int16                                            │
│ 0xCE            │ int32                                            │
│ 0xCF            │ int64                                            │
│ 0xD0            │ str8  (length: uint8)                            │
│ 0xD1            │ str16 (length: uint16)                           │
│ 0xD2            │ str32 (length: uint32)                           │
│ 0xD3            │ array16 (count: uint16)                          │
│ 0xD4            │ array32 (count: uint32)                          │
│ 0xD5            │ map16 (count: uint16 pairs)                      │
│ 0xD6            │ map32 (count: uint32 pairs)                      │
├─────────────────┼──────────────────────────────────────────────────┤
│ 0xE0 - 0xEF     │ Negative fixint (-16 to -1)                      │
│ 0xF0 - 0xFF     │ COMMANDS (see below)                             │
└─────────────────┴──────────────────────────────────────────────────┘
```

## Commands (0xF0 - 0xFF)

Commands are the distinguishing feature of this format. They allow in-stream definitions that the decoder maintains in a lookup table.

```
┌─────────┬─────────────────┬────────────────────────────────────────┐
│ Marker  │ Command         │ Format                                 │
├─────────┼─────────────────┼────────────────────────────────────────┤
│ 0xF0    │ SET_KEY         │ <varint:id> <string_value>             │
│ 0xF1    │ USE_KEY         │ <varint:id>                            │
│ 0xF2    │ DEFINE_STRUCT   │ <varint:id> <uint8:field_count> <keys> │
│ 0xF3    │ USE_STRUCT      │ <varint:id> <values...>                │
│ 0xF4    │ CLEAR_KEYS      │ (no payload - resets key table)        │
│ 0xF5    │ CLEAR_STRUCTS   │ (no payload - resets struct table)     │
│ 0xF6    │ CLEAR_ALL       │ (no payload - resets all tables)       │
│ 0xF7    │ BEGIN_ARRAY     │ (no payload - starts unbounded array)  │
│ 0xF8    │ END             │ (no payload - ends array or map)       │
│ 0xF9    │ BEGIN_MAP       │ (no payload - starts unbounded map)    │
│ 0xFA-FF │ (reserved)      │ Future extensions                      │
└─────────┴─────────────────┴────────────────────────────────────────┘
```

### Varint Encoding

Variable-length integers are encoded as follows:
- `0x00 - 0x7F`: 1 byte (values 0-127)
- `0x80 + N`:    2 bytes, next byte is value (128-255 + N*256)

More specifically:
```
If value < 128:
    [value]
Else if value < 16384:
    [0x80 | (value >> 8), value & 0xFF]
Else if value < 2097152:
    [0xC0 | (value >> 16), (value >> 8) & 0xFF, value & 0xFF]
Else:
    [0xE0 | (value >> 24), (value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF]
```

## Command Details

### SET_KEY / USE_KEY

Used for interning map keys (commonly repeated field names).

**Encoding:**
```
SET_KEY:  0xF0 <varint:id> <string_value>
USE_KEY:  0xF1 <varint:id>
```

### DEFINE_STRUCT / USE_STRUCT

For highly repetitive object structures, define a struct template.

**Encoding:**
```
DEFINE_STRUCT:  0xF2 <varint:id> <uint8:field_count> <key_refs...>
USE_STRUCT:     0xF3 <varint:id> <values...>
```

## Data Layout

### Integers

All multi-byte integers are stored in **big-endian** (network byte order).

### Floating Point

IEEE 754 format:
- float32: 4 bytes, big-endian
- float64: 8 bytes, big-endian

### Strings

UTF-8 encoded. Length prefix indicates byte count, not character count.

### Binary

Raw bytes with length prefix.

## Security Considerations

1. **Table size limits**: Decoders SHOULD limit table sizes to prevent DoS
2. **Recursion depth**: Decoders SHOULD limit nesting depth
3. **String length**: Decoders SHOULD validate string lengths before allocation

