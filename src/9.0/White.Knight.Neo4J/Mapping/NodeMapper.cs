using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neo4j.Driver;
using White.Knight.Neo4J.Navigations;
using White.Knight.Neo4J.Relationships;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace White.Knight.Neo4J.Mapping
{
    public class NodeMapper<TD>(ILoggerFactory loggerFactory = null) : INodeMapper<TD> where TD : class, new()
    {
        private readonly ILogger _logger =
            (loggerFactory ?? new NullLoggerFactory())
            .CreateLogger<NodeMapper<TD>>();

        public IEnumerable<TD> Perform(
            GraphStrategy<TD> graphStrategy,
            Dictionary<int, char> aliasDictionary,
            IEnumerable<IRecord> records)
        {
            if (records?.FirstOrDefault() == null)
                return [];

            var allRecords =
                records
                    .ToList();

            using var enumerator =
                graphStrategy
                    .RelationshipNavigation
                    .GetNavigationChain()
                    .GetEnumerator();

            if (!enumerator.MoveNext())
                throw new Exception($"Could not map nodes of type {typeof(TD).Name}.");

            // Start with last navigation
            var currentNavigation = enumerator.Current;

            var currentAlias = aliasDictionary[currentNavigation.GetHashCode()];

            var currentNodes =
                allRecords
                    .Select(r => r[currentAlias.ToString()].As<INode>())
                    .Distinct()
                    .ToList();

            var mappedNodes = new List<TD>();

            foreach (var currentNode in currentNodes)
                if (MapNode(currentNavigation.DataType, currentNode.Properties) is TD mappedNode)
                {
                    MapNodeAndChildren(
                        mappedNode,
                        currentNode.ElementId,
                        currentNavigation,
                        aliasDictionary,
                        allRecords
                    );

                    mappedNodes
                        .Add(mappedNode);
                }

            return mappedNodes;
        }

        private void MapNodeAndChildren(
            object currentElement,
            string currentElementId,
            IRelationshipNavigation currentNavigation,
            Dictionary<int, char> aliasDictionary,
            ICollection<IRecord> allRecords)
        {
            try
            {
                if (allRecords?.FirstOrDefault() == null) return;

                if (currentNavigation == RelationshipNavigation.Empty) return;

                var currentAlias = aliasDictionary[currentNavigation.GetHashCode()];

                var nextNavigation = currentNavigation.Next();

                var nextAlias = aliasDictionary[nextNavigation.GetHashCode()];

                var nextNodes =
                    allRecords
                        .Select(r => r[nextAlias.ToString()].As<INode>())
                        .ToList();

                var relationshipAlias = $"{currentAlias}_{nextAlias}";

                var currentRelationships =
                    allRecords
                        .Select(r => r[relationshipAlias].As<IRelationship>())
                        .ToList();

                var applicableRelationships =
                    currentRelationships
                        .Where(o => o.StartNodeElementId == currentElementId)
                        .Select(o => o.EndNodeElementId)
                        .ToList();

                var applicableNextNodes =
                    nextNodes
                        .Where(s => applicableRelationships.Contains(s.ElementId))
                        .ToList();

                foreach (var applicableNextNode in applicableNextNodes)
                {
                    // Create
                    var objectToAdd =
                        MapNode(nextNavigation.DataType, applicableNextNode.Properties);

                    // Link current to next
                    currentNavigation
                        .InvokeDescendantSetters(currentElement, objectToAdd);

                    // Drill down to next and repeat
                    MapNodeAndChildren(
                        objectToAdd,
                        applicableNextNode.ElementId,
                        nextNavigation,
                        aliasDictionary,
                        allRecords);
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger
                    .LogWarning("Could not find key in alias dictionary: {message}", ex.Message);
            }
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