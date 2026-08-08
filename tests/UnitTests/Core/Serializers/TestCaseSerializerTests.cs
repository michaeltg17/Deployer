using AwesomeAssertions;
using Core.Testing.Serializers;
using Xunit;

namespace UnitTests.Core.Serializers;

public class TestCaseSerializerTests
{
    readonly TestCaseSerializer serializer = new();

    public class FieldsOnly
    {
        public string Name;
        public int Count;
        public double? Ratio;
    }

    public sealed class PropertiesOnly
    {
        public string Label { get; init; } = "";
        public int Value { get; init; }
        public int[] Numbers { get; init; } = [];
        public bool IsValid => Numbers.Length == 0;
    }

    public class Mixed
    {
        public string Field1;
        public int Field2;
        public string Prop1 { get; init; } = "";
        public double? Prop2 { get; init; }
        public bool Derived => Field2 > 0;
    }

    public class ContainsArrayAndNullable
    {
        public string? Name;
        public string[] Tags { get; init; } = [];
    }

    [Fact]
    public void FieldsOnly_roundTrip()
    {
        var orig = new FieldsOnly { Name = "hello", Count = 42, Ratio = 3.14 };
        var json = serializer.Serialize(orig);
        var result = (FieldsOnly)serializer.Deserialize(typeof(FieldsOnly), json);

        result.Name.Should().Be("hello");
        result.Count.Should().Be(42);
        result.Ratio.Should().Be(3.14);
    }

    [Fact]
    public void FieldsOnly_nullNullableField_roundTrip()
    {
        var orig = new FieldsOnly { Name = "test", Count = 0, Ratio = null };
        var json = serializer.Serialize(orig);
        var result = (FieldsOnly)serializer.Deserialize(typeof(FieldsOnly), json);

        result.Name.Should().Be("test");
        result.Count.Should().Be(0);
        result.Ratio.Should().BeNull();
    }

    [Fact]
    public void PropertiesOnly_roundTrip()
    {
        var orig = new PropertiesOnly { Label = "x", Value = 10, Numbers = [1, 2, 3] };
        var json = serializer.Serialize(orig);
        var result = (PropertiesOnly)serializer.Deserialize(typeof(PropertiesOnly), json);

        result.Label.Should().Be("x");
        result.Value.Should().Be(10);
        result.Numbers.Should().Equal(1, 2, 3);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PropertiesOnly_emptyArray_roundTrip()
    {
        var orig = new PropertiesOnly { Label = "y", Value = 0 };
        var json = serializer.Serialize(orig);
        var result = (PropertiesOnly)serializer.Deserialize(typeof(PropertiesOnly), json);

        result.Label.Should().Be("y");
        result.Value.Should().Be(0);
        result.Numbers.Should().BeEmpty();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Mixed_fieldsAndProperties_roundTrip()
    {
        var orig = new Mixed { Field1 = "a", Field2 = 5, Prop1 = "b", Prop2 = 1.5 };
        var json = serializer.Serialize(orig);
        var result = (Mixed)serializer.Deserialize(typeof(Mixed), json);

        result.Field1.Should().Be("a");
        result.Field2.Should().Be(5);
        result.Prop1.Should().Be("b");
        result.Prop2.Should().Be(1.5);
        result.Derived.Should().BeTrue();
    }

    [Fact]
    public void Mixed_nullNullable_roundTrip()
    {
        var orig = new Mixed { Field1 = "c", Field2 = 0, Prop1 = "", Prop2 = null };
        var json = serializer.Serialize(orig);
        var result = (Mixed)serializer.Deserialize(typeof(Mixed), json);

        result.Field1.Should().Be("c");
        result.Field2.Should().Be(0);
        result.Prop1.Should().Be("");
        result.Prop2.Should().BeNull();
        result.Derived.Should().BeFalse();
    }

    [Fact]
    public void Properties_withTupleArray_roundTrip()
    {
        var orig = new ClassWithTupleArray { Errors = [("Project", "must not be empty."), ("Tag", "must not be empty.")] };
        var json = serializer.Serialize(orig);
        var result = (ClassWithTupleArray)serializer.Deserialize(typeof(ClassWithTupleArray), json);

        result.Errors.Should().HaveCount(2);
        result.Errors[0].Should().Be(("Project", "must not be empty."));
        result.Errors[1].Should().Be(("Tag", "must not be empty."));
    }

    [Fact]
    public void Properties_withTupleArray_empty_roundTrip()
    {
        var orig = new ClassWithTupleArray();
        var json = serializer.Serialize(orig);
        var result = (ClassWithTupleArray)serializer.Deserialize(typeof(ClassWithTupleArray), json);

        result.Errors.Should().BeEmpty();
    }

    public class ClassWithTupleArray
    {
        public (string Property, string Message)[] Errors { get; init; } = [];
    }

    [Fact]
    public void ContainsArrayAndNullable_nonNull_roundTrip()
    {
        var orig = new ContainsArrayAndNullable { Name = "test", Tags = ["a", "b"] };
        var json = serializer.Serialize(orig);
        var result = (ContainsArrayAndNullable)serializer.Deserialize(typeof(ContainsArrayAndNullable), json);

        result.Name.Should().Be("test");
        result.Tags.Should().Equal("a", "b");
    }

    [Fact]
    public void ContainsArrayAndNullable_nullValues_roundTrip()
    {
        var orig = new ContainsArrayAndNullable { Name = null };
        var json = serializer.Serialize(orig);
        var result = (ContainsArrayAndNullable)serializer.Deserialize(typeof(ContainsArrayAndNullable), json);

        result.Name.Should().BeNull();
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public void IsSerializable_alwaysReturnsTrue()
    {
        serializer.IsSerializable(typeof(string), "x", out var reason).Should().BeTrue();
        reason.Should().BeNull();

        serializer.IsSerializable(typeof(int), null, out reason).Should().BeTrue();
        reason.Should().BeNull();

        serializer.IsSerializable(typeof(object), new object(), out reason).Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public void LiteralFields_excluded()
    {
        var orig = new ClassWithLiteral { Value = 10 };
        var json = serializer.Serialize(orig);
        var result = (ClassWithLiteral)serializer.Deserialize(typeof(ClassWithLiteral), json);

        result.Value.Should().Be(10);
    }

    public class ClassWithLiteral
    {
        public const int Constant = 42;
        public static readonly int Static = 99;
        public int Value;
    }
}
