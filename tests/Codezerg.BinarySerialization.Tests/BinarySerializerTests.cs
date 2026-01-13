namespace Codezerg.BinarySerialization.Tests;

public class BinarySerializerTests
{
    #region Test Classes

    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    public class PersonWithIgnore
    {
        public string Name { get; set; } = "";

        [BinaryIgnore]
        public string Secret { get; set; } = "";
    }

    public class PersonWithCustomName
    {
        [BinaryProperty("full_name")]
        public string Name { get; set; } = "";

        [BinaryProperty("years_old")]
        public int Age { get; set; }
    }

    public class PersonWithOrder
    {
        [BinaryProperty(Order = 2)]
        public string Name { get; set; } = "";

        [BinaryProperty(Order = 1)]
        public int Age { get; set; }
    }

    public class NestedObject
    {
        public string Title { get; set; } = "";
        public Person? Author { get; set; }
    }

    public class WithCollections
    {
        public List<string> Tags { get; set; } = new();
        public int[] Numbers { get; set; } = Array.Empty<int>();
        public Dictionary<string, int> Scores { get; set; } = new();
    }

    public class WithNullables
    {
        public int? OptionalInt { get; set; }
        public DateTime? OptionalDate { get; set; }
    }

    public enum Status { Pending, Active, Completed }

    public class WithEnum
    {
        public Status Status { get; set; }
    }

    public class WithSpecialTypes
    {
        public DateTime Created { get; set; }
        public TimeSpan Duration { get; set; }
        public Guid Id { get; set; }
        public decimal Price { get; set; }
    }

    #endregion

    [Fact]
    public void SerializeDeserialize_SimpleObject()
    {
        var original = new Person { Name = "Alice", Age = 30, IsActive = true };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<Person>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal("Alice", deserialized.Name);
        Assert.Equal(30, deserialized.Age);
        Assert.True(deserialized.IsActive);
    }

    [Fact]
    public void SerializeDeserialize_BinaryIgnore()
    {
        var original = new PersonWithIgnore { Name = "Alice", Secret = "TopSecret" };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<PersonWithIgnore>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal("Alice", deserialized.Name);
        Assert.Equal("", deserialized.Secret); // Should be default, not serialized
    }

    [Fact]
    public void SerializeDeserialize_CustomPropertyName()
    {
        var original = new PersonWithCustomName { Name = "Bob", Age = 25 };

        var bytes = BinarySerializer.Serialize(original);

        // Verify it uses custom names by checking the raw data
        using var stream = new MemoryStream(bytes);
        using var reader = new BinarySerializationReader(stream);

        var count = reader.ReadMapHeader();
        Assert.Equal(2, count);

        var key1 = reader.ReadKey();
        Assert.Equal("full_name", key1);
    }

    [Fact]
    public void SerializeDeserialize_NestedObject()
    {
        var original = new NestedObject
        {
            Title = "My Book",
            Author = new Person { Name = "Charlie", Age = 40, IsActive = true }
        };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<NestedObject>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal("My Book", deserialized.Title);
        Assert.NotNull(deserialized.Author);
        Assert.Equal("Charlie", deserialized.Author.Name);
        Assert.Equal(40, deserialized.Author.Age);
    }

    [Fact]
    public void SerializeDeserialize_NullNestedObject()
    {
        var original = new NestedObject { Title = "No Author", Author = null };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<NestedObject>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal("No Author", deserialized.Title);
        Assert.Null(deserialized.Author);
    }

    [Fact]
    public void SerializeDeserialize_Collections()
    {
        var original = new WithCollections
        {
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Numbers = new[] { 1, 2, 3, 4, 5 },
            Scores = new Dictionary<string, int> { { "math", 90 }, { "science", 85 } }
        };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<WithCollections>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Tags.Count);
        Assert.Equal("tag1", deserialized.Tags[0]);
        Assert.Equal(5, deserialized.Numbers.Length);
        Assert.Equal(1, deserialized.Numbers[0]);
        Assert.Equal(90, deserialized.Scores["math"]);
    }

    [Fact]
    public void SerializeDeserialize_Nullables()
    {
        var original = new WithNullables { OptionalInt = 42, OptionalDate = null };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<WithNullables>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(42, deserialized.OptionalInt);
        Assert.Null(deserialized.OptionalDate);
    }

    [Fact]
    public void SerializeDeserialize_Enum()
    {
        var original = new WithEnum { Status = Status.Active };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<WithEnum>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(Status.Active, deserialized.Status);
    }

    [Fact]
    public void SerializeDeserialize_SpecialTypes()
    {
        var original = new WithSpecialTypes
        {
            Created = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(2.5),
            Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Price = 99.99m
        };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<WithSpecialTypes>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Created, deserialized.Created);
        Assert.Equal(original.Duration, deserialized.Duration);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Price, deserialized.Price);
    }

    [Fact]
    public void SerializeDeserialize_Primitives()
    {
        Assert.Equal(42, BinarySerializer.Deserialize<int>(BinarySerializer.Serialize(42)));
        Assert.Equal("hello", BinarySerializer.Deserialize<string>(BinarySerializer.Serialize("hello")));
        Assert.True(BinarySerializer.Deserialize<bool>(BinarySerializer.Serialize(true)));
        Assert.Equal(3.14, BinarySerializer.Deserialize<double>(BinarySerializer.Serialize(3.14)));
    }

    [Fact]
    public void SerializeDeserialize_Array()
    {
        var original = new[] { 1, 2, 3, 4, 5 };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<int[]>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void SerializeDeserialize_List()
    {
        var original = new List<string> { "a", "b", "c" };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<List<string>>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void SerializeDeserialize_Dictionary()
    {
        var original = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };

        var bytes = BinarySerializer.Serialize(original);
        var deserialized = BinarySerializer.Deserialize<Dictionary<string, int>>(bytes);

        Assert.NotNull(deserialized);
        Assert.Equal(1, deserialized["one"]);
        Assert.Equal(2, deserialized["two"]);
    }

    [Fact]
    public void Serialize_WithKeyInterning_SmallerOutput()
    {
        var items = Enumerable.Range(0, 100)
            .Select(i => new Person { Name = $"Person{i}", Age = i, IsActive = true })
            .ToList();

        var withInterning = BinarySerializer.Serialize(items, new SerializerOptions { UseKeyInterning = true });
        var withoutInterning = BinarySerializer.Serialize(items, new SerializerOptions { UseKeyInterning = false });

        Assert.True(withInterning.Length < withoutInterning.Length,
            $"With interning: {withInterning.Length}, Without: {withoutInterning.Length}");
    }
}
