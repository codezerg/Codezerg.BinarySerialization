namespace Codezerg.BinarySerialization.Tests;

public class ResiliencyTests
{
    // Source class with more properties
    public class PersonV2
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Email { get; set; } = "";  // Extra property
        public string Phone { get; set; } = "";  // Extra property
    }

    // Target class with fewer properties
    public class PersonV1
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    // Target class with extra properties not in source
    public class PersonV3
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Country { get; set; } = "Unknown";  // Not in source, has default
        public bool IsVerified { get; set; } = true;       // Not in source, has default
    }

    [Fact]
    public void Deserialize_ExtraPropertiesInData_AreIgnored()
    {
        // Serialize with more properties
        var source = new PersonV2
        {
            Name = "Alice",
            Age = 30,
            Email = "alice@example.com",
            Phone = "555-1234"
        };
        var bytes = BinarySerializer.Serialize(source);

        // Deserialize to class with fewer properties - extra should be ignored
        var target = BinarySerializer.Deserialize<PersonV1>(bytes);

        Assert.NotNull(target);
        Assert.Equal("Alice", target.Name);
        Assert.Equal(30, target.Age);
    }

    [Fact]
    public void Deserialize_MissingPropertiesInData_KeepDefaults()
    {
        // Serialize with fewer properties
        var source = new PersonV1 { Name = "Bob", Age = 25 };
        var bytes = BinarySerializer.Serialize(source);

        // Deserialize to class with more properties - missing should keep defaults
        var target = BinarySerializer.Deserialize<PersonV3>(bytes);

        Assert.NotNull(target);
        Assert.Equal("Bob", target.Name);
        Assert.Equal(25, target.Age);
        Assert.Equal("Unknown", target.Country);  // Default value preserved
        Assert.True(target.IsVerified);            // Default value preserved
    }

    [Fact]
    public void Deserialize_CompletelyDifferentProperties_Works()
    {
        // Serialize one class
        var source = new { Foo = "bar", Baz = 123 };

        // Use low-level API to create data with unknown properties
        using var stream = new MemoryStream();
        using (var writer = new BinarySerializationWriter(stream, leaveOpen: true))
        {
            writer.WriteMapHeader(2);
            writer.Write("UnknownProp1");
            writer.Write("value1");
            writer.Write("UnknownProp2");
            writer.Write(999);
            writer.Flush();
        }

        stream.Position = 0;

        // Deserialize to PersonV1 - all properties should be skipped, defaults used
        var target = BinarySerializer.Deserialize<PersonV1>(stream.ToArray());

        Assert.NotNull(target);
        Assert.Equal("", target.Name);  // Default
        Assert.Equal(0, target.Age);     // Default
    }

    [Fact]
    public void Deserialize_EmptyObject_Works()
    {
        // Create empty map
        using var stream = new MemoryStream();
        using (var writer = new BinarySerializationWriter(stream, leaveOpen: true))
        {
            writer.WriteMapHeader(0);
            writer.Flush();
        }

        var target = BinarySerializer.Deserialize<PersonV3>(stream.ToArray());

        Assert.NotNull(target);
        Assert.Equal("", target.Name);
        Assert.Equal(0, target.Age);
        Assert.Equal("Unknown", target.Country);  // Default preserved
        Assert.True(target.IsVerified);            // Default preserved
    }

    [Fact]
    public void Deserialize_NestedObject_ExtraPropertiesIgnored()
    {
        // Nested classes
        var source = new ContainerV2
        {
            Id = 1,
            Person = new PersonV2 { Name = "Charlie", Age = 35, Email = "c@test.com", Phone = "555" },
            ExtraData = "should be ignored"
        };

        var bytes = BinarySerializer.Serialize(source);
        var target = BinarySerializer.Deserialize<ContainerV1>(bytes);

        Assert.NotNull(target);
        Assert.Equal(1, target.Id);
        Assert.NotNull(target.Person);
        Assert.Equal("Charlie", target.Person.Name);
        Assert.Equal(35, target.Person.Age);
    }

    public class ContainerV2
    {
        public int Id { get; set; }
        public PersonV2? Person { get; set; }
        public string ExtraData { get; set; } = "";
    }

    public class ContainerV1
    {
        public int Id { get; set; }
        public PersonV1? Person { get; set; }
    }

    [Fact]
    public void Deserialize_ListWithExtraProperties_Works()
    {
        var source = new List<PersonV2>
        {
            new() { Name = "A", Age = 1, Email = "a@test.com", Phone = "111" },
            new() { Name = "B", Age = 2, Email = "b@test.com", Phone = "222" }
        };

        var bytes = BinarySerializer.Serialize(source);
        var target = BinarySerializer.Deserialize<List<PersonV1>>(bytes);

        Assert.NotNull(target);
        Assert.Equal(2, target.Count);
        Assert.Equal("A", target[0].Name);
        Assert.Equal(1, target[0].Age);
        Assert.Equal("B", target[1].Name);
        Assert.Equal(2, target[1].Age);
    }

    [Fact]
    public void Deserialize_DictionaryWithExtraProperties_Works()
    {
        var source = new Dictionary<string, PersonV2>
        {
            ["first"] = new() { Name = "X", Age = 10, Email = "x@test.com", Phone = "000" }
        };

        var bytes = BinarySerializer.Serialize(source);
        var target = BinarySerializer.Deserialize<Dictionary<string, PersonV1>>(bytes);

        Assert.NotNull(target);
        Assert.Single(target);
        Assert.Equal("X", target["first"].Name);
        Assert.Equal(10, target["first"].Age);
    }

    [Fact]
    public void Deserialize_PropertyTypeMismatch_Skipped()
    {
        // Create data where "Age" is a string instead of int
        using var stream = new MemoryStream();
        using (var writer = new BinarySerializationWriter(stream, leaveOpen: true))
        {
            writer.WriteMapHeader(2);
            writer.Write("Name");
            writer.Write("TestName");
            writer.Write("Age");
            writer.Write("not a number");  // Wrong type!
            writer.Flush();
        }

        // This should not throw - it should skip the mismatched property
        var target = BinarySerializer.Deserialize<PersonV1>(stream.ToArray());

        Assert.NotNull(target);
        Assert.Equal("TestName", target.Name);
        Assert.Equal(0, target.Age);  // Default because type mismatch
    }
}
