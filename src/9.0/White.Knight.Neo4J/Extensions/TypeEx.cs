using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace White.Knight.Neo4J.Extensions
{
    public static class TypeEx
    {
        public static IEnumerable<Tuple<string, string, string>> BuildNeo4JCommandMapping<T>(this T value)
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
                        .Name;
                        // Using the below, the field cannot be retrieved when calling .AsObject<T>()
                        //.GetMemberPropertyOrJsonAlias();

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