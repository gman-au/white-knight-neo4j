using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver;
using White.Knight.Neo4J.Navigations;
using White.Knight.Neo4J.Relationships;

namespace White.Knight.Neo4J.Mapping
{
    public class NodeMapper<TD> : INodeMapper<TD> where TD : new()
    {
        public IEnumerable<TD> Perform(
            GraphStrategy<TD> graphStrategy,
            Dictionary<int, char> aliasDictionary,
            params IRecord[] records)
        {
            using var enumerator =
                graphStrategy
                    .RelationshipNavigation
                    .GetNavigationChain()
                    .GetEnumerator();

            if (!enumerator.MoveNext())
                throw new Exception($"Could not map nodes of type {typeof(TD).Name}.");

            // Start with primary navigation
            var primaryNavigation = enumerator.Current;

            var primaryAlias = aliasDictionary[primaryNavigation.GetHashCode()];

            var primaryNodes =
                records
                    .Select(r => r[primaryAlias.ToString()].As<INode>())
                    .Distinct()
                    .ToList();

            while (enumerator.MoveNext())
            {
                foreach (var primaryNode in primaryNodes)
                {
                    var secondaryNavigation = enumerator.Current;

                    var secondaryAlias =
                        aliasDictionary[secondaryNavigation.GetHashCode()];

                    var relationshipAlias = $"{primaryAlias}_{secondaryAlias}";

                    var relationships =
                        records
                            .Select(r => r[relationshipAlias].As<IRelationship>())
                            .ToList();

                    var secondaryNodes =
                        records
                            .Select(r => r[secondaryAlias.ToString()].As<INode>())
                            .ToList();

                    var applicableRelationships =
                        relationships
                            .Where(o => o.StartNodeElementId == primaryNode.ElementId)
                            .Select(o => o.EndNodeElementId)
                            .ToList();

                    var applicableSecondaries =
                        secondaryNodes
                            .Where(s => applicableRelationships.Contains(s.ElementId));

                    var objectsToAdd =
                        applicableSecondaries
                            .Select(o => MapNode(secondaryNavigation.DataType, o.Properties))
                            .ToList();
                }
            }

            var mappedNodes =
                primaryNodes
                    .Select(o =>
                        MapNode<TD>(o, o.Properties)
                    )
                    .ToList();

            return mappedNodes;
        }

        private static T MapNode<T>(
            INode node,
            IReadOnlyDictionary<string, object> props
        )
            where T : new()
        {
            var obj = new T();
            var type = typeof(T);

            return (T)SetProps(type, obj, props);
        }

        private static object MapNode(
            Type type,
            IReadOnlyDictionary<string, object> props
        )
        {
            var obj = Activator.CreateInstance(type);

            return SetProps(type, obj, props);
        }

        private static object SetProps(
            Type type,
            object obj,
            IReadOnlyDictionary<string, object> props
        )
        {
            foreach (var prop in type.GetProperties())
                if (props.TryGetValue(prop.Name, out var value))
                {
                    // Handle type conversion based on property type
                    if (prop.PropertyType == typeof(Guid))
                        prop.SetValue(obj, Guid.Parse(value.ToString()));
                    else if (prop.PropertyType == typeof(int))
                        prop.SetValue(obj, int.Parse(value.ToString()));
                    else if (prop.PropertyType == typeof(DateTime))
                        prop.SetValue(obj, DateTime.Parse(value.ToString()));
                    else if (prop.PropertyType == typeof(bool))
                        prop.SetValue(obj, bool.Parse(value.ToString()));
                    else
                        prop.SetValue(obj, value);
                }

            // If property doesn't exist in node, leave as default
            return obj;
        }
    }
}