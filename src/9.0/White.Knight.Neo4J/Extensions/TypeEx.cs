using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using White.Knight.Abstractions.Extensions;

namespace White.Knight.Neo4J.Extensions
{
    public static class TypeEx
    {
        public static IEnumerable<Tuple<string, string, string>> BuildNeo4jCommandMapping<T>(this T value)
        {
            var commandMappings = new List<Tuple<string, string, string>>();

            // expand parameter set map
            foreach (var propertyInfo in FilterTypes(typeof(T)))
            {
                var propertyValue =
                    propertyInfo
                        .GetValue(value);

                var propertyName =
                    propertyInfo
                        .GetMemberPropertyOrJsonAlias();

                if (propertyValue != null)
                    commandMappings
                        .Add(
                            new Tuple<string, string, string>(
                                propertyName.ToLower(),
                                propertyName,
                                propertyValue.ToString()
                            ));
            }

            return commandMappings;
        }

        private static IEnumerable<PropertyInfo> FilterTypes(Type theType)
        {
            return
                theType
                    .GetProperties()
                    .Where(prop =>
                        prop.PropertyType.Namespace == "System" ||
                        prop.PropertyType.IsPrimitive)
                    .ToArray();
        }
    }
}