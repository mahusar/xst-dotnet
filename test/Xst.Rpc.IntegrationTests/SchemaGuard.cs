using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Xst.Rpc.IntegrationTests
{
    internal static class SchemaGuard
    {
        internal static void AssertFullyMapped<T>(JToken raw, string context = null)
        {
            if (raw == null || raw.Type == JTokenType.Null) return;

            var unmapped = FindUnmapped(typeof(T), raw, context ?? typeof(T).Name).ToList();

            Assert.True(unmapped.Count == 0,
                "The daemon returned fields that " + typeof(T).Name + " does not map:" +
                Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", unmapped) +
                Environment.NewLine +
                "Add the properties, or record deliberately ignored fields here.");
        }

        private static IEnumerable<string> FindUnmapped(Type type, JToken token, string path)
        {
            if (token is JArray array)
            {
                var element = ElementType(type);
                if (element == null) yield break;

                var index = 0;
                foreach (var item in array.Take(5))
                {
                    foreach (var miss in FindUnmapped(element, item, path + "[" + index + "]"))
                    {
                        yield return miss;
                    }
                    index++;
                }

                yield break;
            }

            if (!(token is JObject obj)) yield break;
            if (IsOpaque(type)) yield break;

            var known = MappedNames(type);

            foreach (var property in obj.Properties())
            {
                if (!known.TryGetValue(property.Name, out var member))
                {
                    yield return path + "." + property.Name +
                                 "  (" + Describe(property.Value) + ")";
                    continue;
                }

                foreach (var miss in FindUnmapped(member, property.Value, path + "." + property.Name))
                {
                    yield return miss;
                }
            }
        }

        private static Dictionary<string, Type> MappedNames(Type type)
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

                var attribute = property.GetCustomAttribute<JsonPropertyAttribute>();
                var name = attribute?.PropertyName ?? property.Name;

                map[name] = Underlying(property.PropertyType);
            }

            return map;
        }

        private static bool IsOpaque(Type type)
        {
            if (typeof(JToken).IsAssignableFrom(type)) return true;
            if (type == typeof(object)) return true;

            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        private static Type ElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(List<>) || definition == typeof(IReadOnlyList<>) ||
                    definition == typeof(IList<>) || definition == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static Type Underlying(Type type)
        {
            var nullable = Nullable.GetUnderlyingType(type);
            return nullable ?? type;
        }

        private static string Describe(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    return "object";
                case JTokenType.Array:
                    return "array";
                default:
                    var text = value.ToString();
                    return text.Length > 40 ? value.Type + ": " + text.Substring(0, 40) + "..." : value.Type + ": " + text;
            }
        }
    }
}
