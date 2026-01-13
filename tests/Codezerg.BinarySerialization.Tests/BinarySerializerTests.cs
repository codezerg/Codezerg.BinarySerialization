using System.Data;

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

public class DataTableSerializationTests
{
    [Fact]
    public void SerializesEmptyDataTable()
    {
        var table = new DataTable();
        var bytes = BinarySerializer.Serialize(table);
        var result = BinarySerializer.Deserialize<DataTable>(bytes);

        Assert.NotNull(result);
        Assert.Empty(result.Columns);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void SerializesDataTableWithColumnsAndData()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Active", typeof(bool));
        table.Rows.Add(1, "Alice", true);
        table.Rows.Add(2, "Bob", false);

        var bytes = BinarySerializer.Serialize(table);
        var result = BinarySerializer.Deserialize<DataTable>(bytes);

        Assert.NotNull(result);
        Assert.Equal(3, result.Columns.Count);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(1L, result.Rows[0]["Id"]); // integers deserialize as long
        Assert.Equal("Alice", result.Rows[0]["Name"]);
        Assert.Equal(true, result.Rows[0]["Active"]);
        Assert.Equal(2L, result.Rows[1]["Id"]);
        Assert.Equal("Bob", result.Rows[1]["Name"]);
        Assert.Equal(false, result.Rows[1]["Active"]);
    }

    [Fact]
    public void SerializesDataTableWithNullValues()
    {
        var table = new DataTable();
        table.Columns.Add("Value", typeof(string));
        table.Rows.Add(DBNull.Value);

        var bytes = BinarySerializer.Serialize(table);
        var result = BinarySerializer.Deserialize<DataTable>(bytes);

        Assert.NotNull(result);
        Assert.Equal(DBNull.Value, result.Rows[0]["Value"]);
    }

    [Fact]
    public void SerializesDataTableWithVariousTypes()
    {
        var table = new DataTable();
        table.Columns.Add("Long", typeof(long));
        table.Columns.Add("Double", typeof(double));
        table.Columns.Add("String", typeof(string));
        table.Columns.Add("Bool", typeof(bool));
        table.Columns.Add("Bytes", typeof(byte[]));

        var bytes = new byte[] { 1, 2, 3 };
        table.Rows.Add(123456789L, 3.14159, "hello", true, bytes);

        var serialized = BinarySerializer.Serialize(table);
        var result = BinarySerializer.Deserialize<DataTable>(serialized);

        Assert.NotNull(result);
        Assert.Equal(123456789L, result.Rows[0]["Long"]);
        Assert.Equal(3.14159, result.Rows[0]["Double"]);
        Assert.Equal("hello", result.Rows[0]["String"]);
        Assert.Equal(true, result.Rows[0]["Bool"]);
        Assert.Equal(bytes, result.Rows[0]["Bytes"]);
    }

    [Fact]
    public void SerializesDataSet()
    {
        var ds = new DataSet();
        var table1 = ds.Tables.Add();
        table1.Columns.Add("Id", typeof(int));
        table1.Rows.Add(1);

        var bytes = BinarySerializer.Serialize(ds);
        var result = BinarySerializer.Deserialize<DataSet>(bytes);

        Assert.NotNull(result);
        Assert.Single(result.Tables);
        Assert.Equal(1L, result.Tables[0].Rows[0]["Id"]);
    }

    [Fact]
    public void SerializesDataSetWithMultipleTables()
    {
        var ds = new DataSet();

        var users = ds.Tables.Add();
        users.Columns.Add("Id", typeof(int));
        users.Columns.Add("Name", typeof(string));
        users.Rows.Add(1, "Alice");
        users.Rows.Add(2, "Bob");

        var orders = ds.Tables.Add();
        orders.Columns.Add("OrderId", typeof(int));
        orders.Columns.Add("UserId", typeof(int));
        orders.Columns.Add("Amount", typeof(double));
        orders.Rows.Add(100, 1, 99.99);
        orders.Rows.Add(101, 2, 149.50);

        var bytes = BinarySerializer.Serialize(ds);
        var result = BinarySerializer.Deserialize<DataSet>(bytes);

        Assert.NotNull(result);
        Assert.Equal(2, result.Tables.Count);

        Assert.Equal(2, result.Tables[0].Rows.Count);
        Assert.Equal("Alice", result.Tables[0].Rows[0]["Name"]);

        Assert.Equal(2, result.Tables[1].Rows.Count);
        Assert.Equal(99.99, result.Tables[1].Rows[0]["Amount"]);
    }

    [Fact]
    public void SerializesDataTableWithListColumn()
    {
        var table = new DataTable();
        table.Columns.Add("Tags", typeof(object));
        table.Rows.Add(new object[] { new List<string> { "a", "b", "c" } });

        var bytes = BinarySerializer.Serialize(table);
        var result = BinarySerializer.Deserialize<DataTable>(bytes);

        Assert.NotNull(result);
        var tags = result.Rows[0]["Tags"] as List<object?>;
        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Equal("a", tags[0]);
        Assert.Equal("b", tags[1]);
        Assert.Equal("c", tags[2]);
    }

    [Fact]
    public void SerializesDataTableWithDictionaryColumn()
    {
        var table = new DataTable();
        table.Columns.Add("Data", typeof(object));
        table.Rows.Add(new object[] { new Dictionary<string, int> { { "x", 1 }, { "y", 2 } } });

        var bytes = BinarySerializer.Serialize(table);
        var result = BinarySerializer.Deserialize<DataTable>(bytes);

        Assert.NotNull(result);
        var data = result.Rows[0]["Data"] as Dictionary<string, object?>;
        Assert.NotNull(data);
        Assert.Equal(2, data.Count);
        Assert.Equal(1L, data["x"]); // integers deserialize as long
        Assert.Equal(2L, data["y"]);
    }

    [Fact]
    public void SerializesDataTableWithFirstRowAllNulls()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Value", typeof(double));
        table.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value);
        table.Rows.Add(1, "Alice", 3.14);
        table.Rows.Add(2, "Bob", 2.71);

        var bytes = BinarySerializer.Serialize(table);
        var result = BinarySerializer.Deserialize<DataTable>(bytes);

        Assert.NotNull(result);
        Assert.Equal(3, result.Columns.Count);
        Assert.Equal(3, result.Rows.Count);

        // First row - all nulls
        Assert.Equal(DBNull.Value, result.Rows[0]["Id"]);
        Assert.Equal(DBNull.Value, result.Rows[0]["Name"]);
        Assert.Equal(DBNull.Value, result.Rows[0]["Value"]);

        // Second row - has values
        Assert.Equal(1L, result.Rows[1]["Id"]);
        Assert.Equal("Alice", result.Rows[1]["Name"]);
        Assert.Equal(3.14, result.Rows[1]["Value"]);

        // Third row - has values
        Assert.Equal(2L, result.Rows[2]["Id"]);
        Assert.Equal("Bob", result.Rows[2]["Name"]);
        Assert.Equal(2.71, result.Rows[2]["Value"]);
    }

    [Fact]
    public void SerializesIDataReader()
    {
        // Create a DataTable and get its DataReader
        var sourceTable = new DataTable();
        sourceTable.Columns.Add("Id", typeof(int));
        sourceTable.Columns.Add("Name", typeof(string));
        sourceTable.Columns.Add("Active", typeof(bool));
        sourceTable.Rows.Add(1, "Alice", true);
        sourceTable.Rows.Add(2, "Bob", false);
        sourceTable.Rows.Add(3, DBNull.Value, true);

        using var dataReader = sourceTable.CreateDataReader();

        // Serialize the IDataReader
        var bytes = BinarySerializer.Serialize<IDataReader>(dataReader);

        // Deserialize as DataTable
        var result = BinarySerializer.Deserialize<DataTable>(bytes);

        Assert.NotNull(result);
        Assert.Equal(3, result.Columns.Count);
        Assert.Equal(3, result.Rows.Count);

        Assert.Equal(1L, result.Rows[0]["Id"]);
        Assert.Equal("Alice", result.Rows[0]["Name"]);
        Assert.Equal(true, result.Rows[0]["Active"]);

        Assert.Equal(2L, result.Rows[1]["Id"]);
        Assert.Equal("Bob", result.Rows[1]["Name"]);
        Assert.Equal(false, result.Rows[1]["Active"]);

        Assert.Equal(3L, result.Rows[2]["Id"]);
        Assert.Equal(DBNull.Value, result.Rows[2]["Name"]);
        Assert.Equal(true, result.Rows[2]["Active"]);
    }

    [Fact]
    public void SerializesEmptyIDataReader()
    {
        var sourceTable = new DataTable();
        sourceTable.Columns.Add("Id", typeof(int));
        sourceTable.Columns.Add("Name", typeof(string));

        using var dataReader = sourceTable.CreateDataReader();

        var bytes = BinarySerializer.Serialize<IDataReader>(dataReader);
        var result = BinarySerializer.Deserialize<DataTable>(bytes);

        Assert.NotNull(result);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void SerializesDictionaryStringObject()
    {
        var dict = new Dictionary<string, object>
        {
            { "name", "Alice" },
            { "age", 30 },
            { "active", true },
            { "score", 3.14 },
            { "tags", new List<string> { "a", "b" } }
        };

        var bytes = BinarySerializer.Serialize(dict);
        var restored = BinarySerializer.Deserialize<Dictionary<string, object>>(bytes);

        Assert.NotNull(restored);
        Assert.Equal(5, restored.Count);
        Assert.Equal("Alice", restored["name"]);
        Assert.Equal(30L, restored["age"]); // integers become long
        Assert.Equal(true, restored["active"]);
        Assert.Equal(3.14, restored["score"]);
    }

    [Fact]
    public void SerializesDictionaryStringObjectWithNulls()
    {
        var dict = new Dictionary<string, object?>
        {
            { "name", "Bob" },
            { "value", null }
        };

        var bytes = BinarySerializer.Serialize(dict);
        var restored = BinarySerializer.Deserialize<Dictionary<string, object?>>(bytes);

        Assert.NotNull(restored);
        Assert.Equal(2, restored.Count);
        Assert.Equal("Bob", restored["name"]);
        Assert.Null(restored["value"]);
    }

    [Fact]
    public void SerializesDictionaryStringObjectNested()
    {
        var dict = new Dictionary<string, object>
        {
            { "user", new Dictionary<string, object> { { "id", 1 }, { "name", "Alice" } } },
            { "items", new List<object> { 1, "two", 3.0 } }
        };

        var bytes = BinarySerializer.Serialize(dict);
        var restored = BinarySerializer.Deserialize<Dictionary<string, object>>(bytes);

        Assert.NotNull(restored);
        Assert.Equal(2, restored.Count);

        var user = restored["user"] as Dictionary<string, object?>;
        Assert.NotNull(user);
        Assert.Equal(1L, user["id"]);
        Assert.Equal("Alice", user["name"]);

        var items = restored["items"] as List<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        Assert.Equal(1L, items[0]);
        Assert.Equal("two", items[1]);
        Assert.Equal(3.0, items[2]);
    }

    [Fact]
    public void SerializesListOfObject()
    {
        var list = new List<object> { 1, "hello", true, 3.14, null! };

        var bytes = BinarySerializer.Serialize(list);
        var restored = BinarySerializer.Deserialize<List<object>>(bytes);

        Assert.NotNull(restored);
        Assert.Equal(5, restored.Count);
        Assert.Equal(1L, restored[0]);
        Assert.Equal("hello", restored[1]);
        Assert.Equal(true, restored[2]);
        Assert.Equal(3.14, restored[3]);
        Assert.Null(restored[4]);
    }
}
