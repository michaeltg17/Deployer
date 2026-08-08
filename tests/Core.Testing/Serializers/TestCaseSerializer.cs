using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit.Sdk;

namespace Core.Testing.Serializers
{
    public class TestCaseSerializer : IXunitSerializer
    {
        static readonly ConcurrentDictionary<string, Type> TypeCache = new();

        public bool IsSerializable(Type type, object? value, out string? reason)
        {
            reason = null;
            return true;
        }

        public string Serialize(object value)
        {
            var type = value.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsLiteral).ToArray();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite).ToArray();
            var entries = new List<Dictionary<string, string?>>();

            foreach (var field in fields)
            {
                entries.Add(ToJsonEntry(field.FieldType, field.GetValue(value)));
            }

            foreach (var prop in properties)
            {
                entries.Add(ToJsonEntry(prop.PropertyType, prop.GetValue(value)));
            }

            return JsonSerializer.Serialize(entries);
        }

        public object Deserialize(Type type, string data)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsLiteral).ToArray();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite).ToArray();
            var elements = JsonSerializer.Deserialize<JsonElement>(data).EnumerateArray().ToList();
            var instance = CreateInstance(type);

            int idx = 0;
            foreach (var field in fields)
            {
                var entry = ToDictionary(elements[idx]);
                field.SetValue(instance, FromJsonEntry(field.FieldType, entry));
                idx++;
            }

            foreach (var prop in properties)
            {
                var entry = ToDictionary(elements[idx]);
                prop.SetValue(instance, FromJsonEntry(prop.PropertyType, entry));
                idx++;
            }

            return instance;
        }

        static object CreateInstance(Type type)
        {
            // Prefer public constructor if available, fall back to uninitialized
            var ctor = type.GetConstructor(Type.EmptyTypes);
            return ctor != null ? Activator.CreateInstance(type) : RuntimeHelpers.GetUninitializedObject(type);
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
