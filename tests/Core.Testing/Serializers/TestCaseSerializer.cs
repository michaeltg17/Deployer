using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit.Sdk;

namespace Core.Testing.Serializers
{
    public class TestCaseSerializer : IXunitSerializer
    {
        static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();
        static readonly ConcurrentDictionary<string, Type> TypeCache = new();

        public bool IsSerializable(Type type, object? value, out string? reason)
        {
            reason = null;
            return true;
        }

        public string Serialize(object value)
        {
            var type = value.GetType();
            var fields = FieldCache.GetOrAdd(type, t => t.GetFields(BindingFlags.Public | BindingFlags.Instance));
            var entries = new List<Dictionary<string, string?>>();

            foreach (var field in fields)
            {
                var fieldVal = field.GetValue(value);
                entries.Add(ToJsonEntry(field.FieldType, fieldVal));
            }

            return JsonSerializer.Serialize(entries);
        }

        public object Deserialize(Type type, string data)
        {
            var fields = FieldCache.GetOrAdd(type, t => t.GetFields(BindingFlags.Public | BindingFlags.Instance));
            var elements = JsonSerializer.Deserialize<JsonElement>(data).EnumerateArray().ToList();
            var instance = RuntimeHelpers.GetUninitializedObject(type);

            for (int i = 0; i < fields.Length; i++)
            {
                var entry = ToDictionary(elements[i]);
                var value = FromJsonEntry(fields[i].FieldType, entry);
                fields[i].SetValue(instance, value);
            }

            return instance;
        }

        static Dictionary<string, string?> ToJsonEntry(Type type, object? value)
        {
            if (value == null)
                return new() { { "t", "null" }, { "v", null } };

            var typeQn = type.AssemblyQualifiedName ?? type.FullName!;
            TypeCache.TryAdd(typeQn, type);
            return new() { { "t", typeQn }, { "v", JsonSerializer.Serialize(value) } };
        }

        static Dictionary<string, string?> ToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, string?>();
            foreach (var prop in element.EnumerateObject())
                dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
            return dict;
        }

        static object? FromJsonEntry(Type target, Dictionary<string, string?> entry)
        {
            var typeTag = entry["t"]!;

            if (typeTag == "null" || entry["v"] == null)
                return null;

            var resolved = TypeCache.GetOrAdd(typeTag, n => Type.GetType(n)!);
            return JsonSerializer.Deserialize(entry["v"]!, resolved)!;
        }
    }
}
