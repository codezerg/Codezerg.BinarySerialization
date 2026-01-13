namespace Codezerg.BinarySerialization.Tests;

public class BinarySerializationTests
{
    private static (BinarySerializationWriter writer, MemoryStream stream) CreateWriter()
    {
        var stream = new MemoryStream();
        var writer = new BinarySerializationWriter(stream, leaveOpen: true);
        return (writer, stream);
    }

    private static BinarySerializationReader CreateReader(MemoryStream stream)
    {
        stream.Position = 0;
        return new BinarySerializationReader(stream, leaveOpen: true);
    }

    [Fact]
    public void WriteRead_Nil()
    {
        var (writer, stream) = CreateWriter();
        writer.WriteNil();
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(SerializedType.Nil, reader.PeekType());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WriteRead_Boolean(bool value)
    {
        var (writer, stream) = CreateWriter();
        writer.Write(value);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(value, reader.ReadBoolean());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(-1)]
    [InlineData(-16)]
    [InlineData(-100)]
    [InlineData(128)]
    [InlineData(1000)]
    [InlineData(-1000)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void WriteRead_Int32(int value)
    {
        var (writer, stream) = CreateWriter();
        writer.Write(value);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(value, reader.ReadInt32());
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(127L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void WriteRead_Int64(long value)
    {
        var (writer, stream) = CreateWriter();
        writer.Write(value);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(value, reader.ReadInt64());
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(127UL)]
    [InlineData(ulong.MaxValue)]
    public void WriteRead_UInt64(ulong value)
    {
        var (writer, stream) = CreateWriter();
        writer.Write(value);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(value, reader.ReadUInt64());
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(3.14f)]
    [InlineData(-273.15f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    public void WriteRead_Float32(float value)
    {
        var (writer, stream) = CreateWriter();
        writer.Write(value);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(value, reader.ReadSingle());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.14159265358979)]
    [InlineData(-273.15)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void WriteRead_Float64(double value)
    {
        var (writer, stream) = CreateWriter();
        writer.Write(value);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(value, reader.ReadDouble());
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("Hello, World!")]
    [InlineData("Unicode: \u00e9\u00e8\u00ea")]
    public void WriteRead_String(string value)
    {
        var (writer, stream) = CreateWriter();
        writer.Write(value);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(value, reader.ReadString());
    }

    [Fact]
    public void WriteRead_NullString()
    {
        var (writer, stream) = CreateWriter();
        writer.Write((string?)null);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Null(reader.ReadString());
    }

    [Fact]
    public void WriteRead_LongString()
    {
        var value = new string('x', 1000);
        var (writer, stream) = CreateWriter();
        writer.Write(value);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(value, reader.ReadString());
    }

    [Fact]
    public void WriteRead_Binary()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var (writer, stream) = CreateWriter();
        writer.Write(data);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(data, reader.ReadBinary());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(100)]
    public void WriteRead_ArrayHeader(int count)
    {
        var (writer, stream) = CreateWriter();
        writer.WriteArrayHeader(count);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(count, reader.ReadArrayHeader());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(100)]
    public void WriteRead_MapHeader(int count)
    {
        var (writer, stream) = CreateWriter();
        writer.WriteMapHeader(count);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(count, reader.ReadMapHeader());
    }

    [Fact]
    public void WriteRead_Array()
    {
        var (writer, stream) = CreateWriter();
        writer.WriteArrayHeader(3);
        writer.Write(1);
        writer.Write(2);
        writer.Write(3);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(3, reader.ReadArrayHeader());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(2, reader.ReadInt32());
        Assert.Equal(3, reader.ReadInt32());
    }

    [Fact]
    public void WriteRead_Map()
    {
        var (writer, stream) = CreateWriter();
        writer.WriteMapHeader(2);
        writer.Write("name");
        writer.Write("Alice");
        writer.Write("age");
        writer.Write(30);
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(2, reader.ReadMapHeader());
        Assert.Equal("name", reader.ReadString());
        Assert.Equal("Alice", reader.ReadString());
        Assert.Equal("age", reader.ReadString());
        Assert.Equal(30, reader.ReadInt32());
    }

    [Fact]
    public void WriteRead_UnboundedArray()
    {
        var (writer, stream) = CreateWriter();
        writer.BeginArray();
        writer.Write(1);
        writer.Write(2);
        writer.Write(3);
        writer.WriteEnd();
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(-1, reader.ReadArrayHeader());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(2, reader.ReadInt32());
        Assert.Equal(3, reader.ReadInt32());
        reader.ReadEnd();
    }

    [Fact]
    public void WriteRead_UnboundedMap()
    {
        var (writer, stream) = CreateWriter();
        writer.BeginMap();
        writer.Write("key1");
        writer.Write("value1");
        writer.WriteEnd();
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(-1, reader.ReadMapHeader());
        Assert.Equal("key1", reader.ReadString());
        Assert.Equal("value1", reader.ReadString());
        reader.ReadEnd();
    }

    [Fact]
    public void WriteRead_KeyInterning()
    {
        var (writer, stream) = CreateWriter();

        // First use defines the key
        writer.WriteMapHeader(3);
        writer.WriteKey("name");
        writer.Write("Alice");
        writer.WriteKey("name"); // Second use should use reference
        writer.Write("Bob");
        writer.WriteKey("name"); // Third use should use reference
        writer.Write("Charlie");
        writer.Flush();

        using var reader = CreateReader(stream);
        Assert.Equal(3, reader.ReadMapHeader());
        Assert.Equal("name", reader.ReadKey());
        Assert.Equal("Alice", reader.ReadString());
        Assert.Equal("name", reader.ReadKey());
        Assert.Equal("Bob", reader.ReadString());
        Assert.Equal("name", reader.ReadKey());
        Assert.Equal("Charlie", reader.ReadString());
    }

    [Fact]
    public void WriteRead_StructTemplate()
    {
        var (writer, stream) = CreateWriter();

        // Define struct with fields
        var structId = writer.DefineStruct("name", "age", "active");

        // Use struct multiple times
        writer.UseStruct(structId);
        writer.Write("Alice");
        writer.Write(30);
        writer.Write(true);

        writer.UseStruct(structId);
        writer.Write("Bob");
        writer.Write(25);
        writer.Write(false);
        writer.Flush();

        using var reader = CreateReader(stream);

        // First read gets DEFINE_STRUCT - this just defines the template
        var defineFields = reader.ReadStructHeader();
        Assert.Equal(new[] { "name", "age", "active" }, defineFields);

        // Second read gets USE_STRUCT - values follow
        var fields1 = reader.ReadStructHeader();
        Assert.Equal(new[] { "name", "age", "active" }, fields1);
        Assert.Equal("Alice", reader.ReadString());
        Assert.Equal(30, reader.ReadInt32());
        Assert.True(reader.ReadBoolean());

        // Third read gets USE_STRUCT again
        var fields2 = reader.ReadStructHeader();
        Assert.Equal(new[] { "name", "age", "active" }, fields2);
        Assert.Equal("Bob", reader.ReadString());
        Assert.Equal(25, reader.ReadInt32());
        Assert.False(reader.ReadBoolean());
    }

    [Fact]
    public void Skip_Values()
    {
        var (writer, stream) = CreateWriter();
        writer.Write(42);
        writer.Write("hello");
        writer.Write(3.14);
        writer.Flush();

        using var reader = CreateReader(stream);
        reader.Skip(); // Skip integer
        reader.Skip(); // Skip string
        Assert.Equal(3.14, reader.ReadDouble());
    }

    [Fact]
    public void Skip_NestedStructures()
    {
        var (writer, stream) = CreateWriter();
        writer.WriteArrayHeader(2);
        writer.WriteMapHeader(1);
        writer.Write("key");
        writer.Write("value");
        writer.Write(100);
        writer.Write(999);
        writer.Flush();

        using var reader = CreateReader(stream);
        reader.Skip(); // Skip the entire array with nested map
        Assert.Equal(999, reader.ReadInt32());
    }
}
