using Codezerg.BinarySerialization;

Console.WriteLine("=== Codezerg.BinarySerialization Examples ===\n");

// Example 1: High-level object serialization
HighLevelSerializationExample();

// Example 2: Basic types (low-level API)
BasicTypesExample();

// Example 3: Collections (arrays and maps)
CollectionsExample();

// Example 4: Key interning for space savings
KeyInterningExample();

// Example 5: Struct templates for repeated objects
StructTemplateExample();

// Example 6: Streaming with unbounded collections
StreamingExample();

Console.WriteLine("=== All examples completed ===");

void HighLevelSerializationExample()
{
    Console.WriteLine("--- Example 1: High-Level Object Serialization ---");

    // Simple object
    var person = new Person { Name = "Alice", Age = 30, Email = "alice@example.com" };
    var bytes = BinarySerializer.Serialize(person);
    var restored = BinarySerializer.Deserialize<Person>(bytes);

    Console.WriteLine($"Serialized Person to {bytes.Length} bytes");
    Console.WriteLine($"Restored: {restored?.Name}, Age {restored?.Age}");

    // Object with collections
    var order = new Order
    {
        OrderId = Guid.NewGuid(),
        Customer = "Bob's Shop",
        Items = new List<OrderItem>
        {
            new() { ProductName = "Widget", Quantity = 5, Price = 9.99m },
            new() { ProductName = "Gadget", Quantity = 2, Price = 24.99m }
        },
        Tags = new Dictionary<string, string>
        {
            { "priority", "high" },
            { "region", "us-west" }
        },
        CreatedAt = DateTime.UtcNow
    };

    var orderBytes = BinarySerializer.Serialize(order);
    var restoredOrder = BinarySerializer.Deserialize<Order>(orderBytes);

    Console.WriteLine($"\nSerialized Order to {orderBytes.Length} bytes");
    Console.WriteLine($"Order {restoredOrder?.OrderId}");
    Console.WriteLine($"  Customer: {restoredOrder?.Customer}");
    Console.WriteLine($"  Items: {restoredOrder?.Items.Count}");
    foreach (var item in restoredOrder?.Items ?? new())
    {
        Console.WriteLine($"    - {item.Quantity}x {item.ProductName} @ ${item.Price}");
    }

    // Compare with JSON size (approximate)
    var json = System.Text.Json.JsonSerializer.Serialize(order);
    Console.WriteLine($"\nSize comparison:");
    Console.WriteLine($"  Binary: {orderBytes.Length} bytes");
    Console.WriteLine($"  JSON:   {json.Length} bytes");
    Console.WriteLine($"  Savings: {(1 - (double)orderBytes.Length / json.Length) * 100:F1}%");

    Console.WriteLine();
}

void BasicTypesExample()
{
    Console.WriteLine("--- Example 2: Basic Types (Low-Level) ---");

    using var stream = new MemoryStream();

    // Write various types
    using (var writer = new BinarySerializationWriter(stream, leaveOpen: true))
    {
        writer.WriteNil();
        writer.Write(true);
        writer.Write(false);
        writer.Write(42);
        writer.Write(-100);
        writer.Write(long.MaxValue);
        writer.Write(3.14159f);
        writer.Write(2.718281828);
        writer.Write("Hello, World!");
        writer.Write(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
        writer.Flush();
    }

    Console.WriteLine($"Serialized {stream.Length} bytes");

    // Read them back
    stream.Position = 0;
    using var reader = new BinarySerializationReader(stream);

    Console.WriteLine($"Nil type: {reader.PeekType()}");
    reader.Skip(); // Skip nil

    Console.WriteLine($"Boolean 1: {reader.ReadBoolean()}");
    Console.WriteLine($"Boolean 2: {reader.ReadBoolean()}");
    Console.WriteLine($"Int32: {reader.ReadInt32()}");
    Console.WriteLine($"Negative: {reader.ReadInt32()}");
    Console.WriteLine($"Int64: {reader.ReadInt64()}");
    Console.WriteLine($"Float32: {reader.ReadSingle()}");
    Console.WriteLine($"Float64: {reader.ReadDouble()}");
    Console.WriteLine($"String: {reader.ReadString()}");
    Console.WriteLine($"Binary: {BitConverter.ToString(reader.ReadBinary())}");

    Console.WriteLine();
}

void CollectionsExample()
{
    Console.WriteLine("--- Example 3: Collections ---");

    using var stream = new MemoryStream();

    // Write an array and a map
    using (var writer = new BinarySerializationWriter(stream, leaveOpen: true))
    {
        // Fixed-size array
        writer.WriteArrayHeader(3);
        writer.Write("apple");
        writer.Write("banana");
        writer.Write("cherry");

        // Fixed-size map (like a JSON object)
        writer.WriteMapHeader(2);
        writer.Write("name");
        writer.Write("Alice");
        writer.Write("age");
        writer.Write(30);

        writer.Flush();
    }

    Console.WriteLine($"Serialized {stream.Length} bytes");

    // Read them back
    stream.Position = 0;
    using var reader = new BinarySerializationReader(stream);

    // Read array
    var arrayCount = reader.ReadArrayHeader();
    Console.WriteLine($"Array with {arrayCount} elements:");
    for (int i = 0; i < arrayCount; i++)
    {
        Console.WriteLine($"  [{i}]: {reader.ReadString()}");
    }

    // Read map
    var mapCount = reader.ReadMapHeader();
    Console.WriteLine($"Map with {mapCount} pairs:");
    for (int i = 0; i < mapCount; i++)
    {
        var key = reader.ReadString();
        var type = reader.PeekType();
        if (type == SerializedType.String)
            Console.WriteLine($"  {key}: {reader.ReadString()}");
        else if (type == SerializedType.Integer)
            Console.WriteLine($"  {key}: {reader.ReadInt32()}");
    }

    Console.WriteLine();
}

void KeyInterningExample()
{
    Console.WriteLine("--- Example 4: Key Interning ---");

    using var streamWithInterning = new MemoryStream();
    using var streamWithoutInterning = new MemoryStream();

    // Simulate 100 user records with key interning
    using (var writer = new BinarySerializationWriter(streamWithInterning, leaveOpen: true))
    {
        writer.WriteArrayHeader(100);
        for (int i = 0; i < 100; i++)
        {
            writer.WriteMapHeader(3);
            writer.WriteKey("username");  // Interned after first use
            writer.Write($"user{i}");
            writer.WriteKey("email");     // Interned after first use
            writer.Write($"user{i}@example.com");
            writer.WriteKey("active");    // Interned after first use
            writer.Write(i % 2 == 0);
        }
        writer.Flush();
    }

    // Same data without interning
    using (var writer = new BinarySerializationWriter(streamWithoutInterning, leaveOpen: true))
    {
        writer.WriteArrayHeader(100);
        for (int i = 0; i < 100; i++)
        {
            writer.WriteMapHeader(3);
            writer.Write("username");  // Full string every time
            writer.Write($"user{i}");
            writer.Write("email");     // Full string every time
            writer.Write($"user{i}@example.com");
            writer.Write("active");    // Full string every time
            writer.Write(i % 2 == 0);
        }
        writer.Flush();
    }

    Console.WriteLine($"With key interning: {streamWithInterning.Length} bytes");
    Console.WriteLine($"Without interning:  {streamWithoutInterning.Length} bytes");
    Console.WriteLine($"Space saved: {streamWithoutInterning.Length - streamWithInterning.Length} bytes " +
                      $"({(1 - (double)streamWithInterning.Length / streamWithoutInterning.Length) * 100:F1}%)");

    // Read back with interning
    streamWithInterning.Position = 0;
    using var reader = new BinarySerializationReader(streamWithInterning);
    var count = reader.ReadArrayHeader();
    Console.WriteLine($"\nReading first 3 of {count} records:");
    for (int i = 0; i < 3; i++)
    {
        reader.ReadMapHeader();
        Console.WriteLine($"  Record {i}:");
        Console.WriteLine($"    {reader.ReadKey()}: {reader.ReadString()}");
        Console.WriteLine($"    {reader.ReadKey()}: {reader.ReadString()}");
        Console.WriteLine($"    {reader.ReadKey()}: {reader.ReadBoolean()}");
    }

    Console.WriteLine();
}

void StructTemplateExample()
{
    Console.WriteLine("--- Example 5: Struct Templates ---");

    using var stream = new MemoryStream();

    // Define and use struct templates for maximum space efficiency
    using (var writer = new BinarySerializationWriter(stream, leaveOpen: true))
    {
        // Define struct template (writes once, reused many times)
        var personStruct = writer.DefineStruct("name", "age", "city");

        // Write multiple instances - only values are written, no keys!
        writer.WriteArrayHeader(3);

        writer.UseStruct(personStruct);
        writer.Write("Alice");
        writer.Write(30);
        writer.Write("New York");

        writer.UseStruct(personStruct);
        writer.Write("Bob");
        writer.Write(25);
        writer.Write("London");

        writer.UseStruct(personStruct);
        writer.Write("Charlie");
        writer.Write(35);
        writer.Write("Tokyo");

        writer.Flush();
    }

    Console.WriteLine($"Serialized {stream.Length} bytes for 3 person records with struct template");

    // Read them back
    stream.Position = 0;
    using var reader = new BinarySerializationReader(stream);

    // Read struct definition
    var fields = reader.ReadStructHeader();
    Console.WriteLine($"Struct fields: {string.Join(", ", fields)}");

    var arrayCount = reader.ReadArrayHeader();
    Console.WriteLine($"\n{arrayCount} records:");

    for (int i = 0; i < arrayCount; i++)
    {
        var structFields = reader.ReadStructHeader();
        Console.WriteLine($"  Person {i + 1}:");
        Console.WriteLine($"    {structFields[0]}: {reader.ReadString()}");
        Console.WriteLine($"    {structFields[1]}: {reader.ReadInt32()}");
        Console.WriteLine($"    {structFields[2]}: {reader.ReadString()}");
    }

    Console.WriteLine();
}

void StreamingExample()
{
    Console.WriteLine("--- Example 6: Streaming ---");

    using var stream = new MemoryStream();

    // Use unbounded collections for streaming scenarios
    using (var writer = new BinarySerializationWriter(stream, leaveOpen: true))
    {
        // Start an unbounded array - we don't know the count upfront
        writer.BeginArray();

        // Write items as they arrive
        for (int i = 1; i <= 5; i++)
        {
            writer.BeginMap();
            writer.Write("event_id");
            writer.Write(i);
            writer.Write("timestamp");
            writer.Write(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            writer.Write("message");
            writer.Write($"Event {i} occurred");
            writer.WriteEnd(); // End this map
        }

        writer.WriteEnd(); // End the array
        writer.Flush();
    }

    Console.WriteLine($"Serialized {stream.Length} bytes");

    // Read the stream
    stream.Position = 0;
    using var reader = new BinarySerializationReader(stream);

    var arrayLen = reader.ReadArrayHeader();
    Console.WriteLine($"Reading unbounded array (length indicator: {arrayLen}):");

    int eventNum = 0;
    while (!reader.IsEnd())
    {
        eventNum++;
        reader.ReadMapHeader(); // Begin map

        Console.WriteLine($"  Event {eventNum}:");
        while (!reader.IsEnd())
        {
            var key = reader.ReadString();
            var valueType = reader.PeekType();

            if (valueType == SerializedType.Integer)
                Console.WriteLine($"    {key}: {reader.ReadInt64()}");
            else if (valueType == SerializedType.String)
                Console.WriteLine($"    {key}: {reader.ReadString()}");
        }
        reader.ReadEnd(); // End map
    }
    reader.ReadEnd(); // End array

    Console.WriteLine();
}

// Example classes for high-level serialization (must be at end of file)
public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
}

public class Order
{
    public Guid OrderId { get; set; }
    public string Customer { get; set; } = "";
    public List<OrderItem> Items { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class OrderItem
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
