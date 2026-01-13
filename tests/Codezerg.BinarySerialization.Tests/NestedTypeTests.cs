namespace Codezerg.BinarySerialization.Tests;

public class NestedTypeTests
{
    // Nested class inside test class
    public class OuterClass
    {
        public string Name { get; set; } = "";
        public InnerClass? Inner { get; set; }

        // Nested class
        public class InnerClass
        {
            public int Value { get; set; }
            public string Description { get; set; } = "";

            // Deeply nested class
            public class DeeplyNested
            {
                public bool Flag { get; set; }
                public double Amount { get; set; }
            }

            public DeeplyNested? Deep { get; set; }
        }
    }

    // Nested struct
    public struct OuterStruct
    {
        public string Label { get; set; }
        public InnerStruct Inner { get; set; }

        public struct InnerStruct
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
    }

    // Nested enum
    public class ClassWithNestedEnum
    {
        public string Name { get; set; } = "";
        public Status CurrentStatus { get; set; }

        public enum Status
        {
            Unknown = 0,
            Active = 1,
            Inactive = 2,
            Pending = 3
        }
    }

    // Generic nested type
    public class Container<T>
    {
        public T? Value { get; set; }
        public Metadata? Meta { get; set; }

        public class Metadata
        {
            public DateTime Created { get; set; }
            public string CreatedBy { get; set; } = "";
        }
    }

    // Private nested class (accessible via public property)
    public class WithPrivateNested
    {
        public string Id { get; set; } = "";
        public PrivateInner? Data { get; set; }

        // This is public but defined as nested
        public class PrivateInner
        {
            public string Secret { get; set; } = "";
        }
    }

    [Fact]
    public void SerializeDeserialize_NestedClass()
    {
        var original = new OuterClass
        {
            Name = "Outer",
            Inner = new OuterClass.InnerClass
            {
                Value = 42,
                Description = "Inner description"
            }
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<OuterClass>(bytes);

        Assert.NotNull(restored);
        Assert.Equal("Outer", restored.Name);
        Assert.NotNull(restored.Inner);
        Assert.Equal(42, restored.Inner.Value);
        Assert.Equal("Inner description", restored.Inner.Description);
    }

    [Fact]
    public void SerializeDeserialize_DeeplyNestedClass()
    {
        var original = new OuterClass
        {
            Name = "Top",
            Inner = new OuterClass.InnerClass
            {
                Value = 100,
                Description = "Middle",
                Deep = new OuterClass.InnerClass.DeeplyNested
                {
                    Flag = true,
                    Amount = 99.99
                }
            }
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<OuterClass>(bytes);

        Assert.NotNull(restored);
        Assert.Equal("Top", restored.Name);
        Assert.NotNull(restored.Inner);
        Assert.Equal(100, restored.Inner.Value);
        Assert.NotNull(restored.Inner.Deep);
        Assert.True(restored.Inner.Deep.Flag);
        Assert.Equal(99.99, restored.Inner.Deep.Amount);
    }

    [Fact]
    public void SerializeDeserialize_NestedStruct()
    {
        var original = new OuterStruct
        {
            Label = "Point",
            Inner = new OuterStruct.InnerStruct { X = 10, Y = 20 }
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<OuterStruct>(bytes);

        Assert.Equal("Point", restored.Label);
        Assert.Equal(10, restored.Inner.X);
        Assert.Equal(20, restored.Inner.Y);
    }

    [Fact]
    public void SerializeDeserialize_NestedEnum()
    {
        var original = new ClassWithNestedEnum
        {
            Name = "Test",
            CurrentStatus = ClassWithNestedEnum.Status.Active
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<ClassWithNestedEnum>(bytes);

        Assert.NotNull(restored);
        Assert.Equal("Test", restored.Name);
        Assert.Equal(ClassWithNestedEnum.Status.Active, restored.CurrentStatus);
    }

    [Fact]
    public void SerializeDeserialize_GenericWithNestedType()
    {
        var original = new Container<string>
        {
            Value = "Hello",
            Meta = new Container<string>.Metadata
            {
                Created = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                CreatedBy = "System"
            }
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<Container<string>>(bytes);

        Assert.NotNull(restored);
        Assert.Equal("Hello", restored.Value);
        Assert.NotNull(restored.Meta);
        Assert.Equal("System", restored.Meta.CreatedBy);
    }

    [Fact]
    public void SerializeDeserialize_ListOfNestedTypes()
    {
        var original = new List<OuterClass.InnerClass>
        {
            new() { Value = 1, Description = "First" },
            new() { Value = 2, Description = "Second" },
            new() { Value = 3, Description = "Third" }
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<List<OuterClass.InnerClass>>(bytes);

        Assert.NotNull(restored);
        Assert.Equal(3, restored.Count);
        Assert.Equal(1, restored[0].Value);
        Assert.Equal("Second", restored[1].Description);
        Assert.Equal(3, restored[2].Value);
    }

    [Fact]
    public void SerializeDeserialize_DictionaryWithNestedTypes()
    {
        var original = new Dictionary<string, OuterClass.InnerClass>
        {
            ["a"] = new() { Value = 10, Description = "Alpha" },
            ["b"] = new() { Value = 20, Description = "Beta" }
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<Dictionary<string, OuterClass.InnerClass>>(bytes);

        Assert.NotNull(restored);
        Assert.Equal(2, restored.Count);
        Assert.Equal(10, restored["a"].Value);
        Assert.Equal("Beta", restored["b"].Description);
    }

    [Fact]
    public void SerializeDeserialize_ArrayOfNestedTypes()
    {
        var original = new OuterClass.InnerClass[]
        {
            new() { Value = 100, Description = "Item1" },
            new() { Value = 200, Description = "Item2" }
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<OuterClass.InnerClass[]>(bytes);

        Assert.NotNull(restored);
        Assert.Equal(2, restored.Length);
        Assert.Equal(100, restored[0].Value);
        Assert.Equal("Item2", restored[1].Description);
    }

    [Fact]
    public void SerializeDeserialize_NullNestedObject()
    {
        var original = new OuterClass
        {
            Name = "NoInner",
            Inner = null
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<OuterClass>(bytes);

        Assert.NotNull(restored);
        Assert.Equal("NoInner", restored.Name);
        Assert.Null(restored.Inner);
    }

    [Fact]
    public void SerializeDeserialize_PrivateNestedClass()
    {
        var original = new WithPrivateNested
        {
            Id = "123",
            Data = new WithPrivateNested.PrivateInner { Secret = "hidden" }
        };

        var bytes = BinarySerializer.Serialize(original);
        var restored = BinarySerializer.Deserialize<WithPrivateNested>(bytes);

        Assert.NotNull(restored);
        Assert.Equal("123", restored.Id);
        Assert.NotNull(restored.Data);
        Assert.Equal("hidden", restored.Data.Secret);
    }
}
