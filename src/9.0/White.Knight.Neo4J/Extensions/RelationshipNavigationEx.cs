using System;
using System.Linq;
using White.Knight.Neo4J.Relationships;

namespace White.Knight.Neo4J.Extensions
{
    public static class RelationshipNavigationEx
    {
        private const string SetterMethod = "Setter";
        private const string InvokeMethod = "Invoke";

        public static void InvokeDescendantSetters(
            this IRelationshipNavigation sourceNavigation,
            object primaryNode,
            params object[] relatedNodeDescendants)
        {
            var navigationProperties =
                sourceNavigation
                    .GetType()
                    .GetProperties();

            var setter =
                navigationProperties
                    .FirstOrDefault(o => o.Name == SetterMethod);

            if (setter == null) return;

            var action =
                setter
                    .GetValue(sourceNavigation);

            if (action?.GetType() == null) return;

            var actionType =
                action
                    .GetType();

            if (!actionType.IsGenericType || actionType.GetGenericTypeDefinition() != typeof(Action<,>)) return;

            var invokeMethod =
                actionType
                    .GetMethod(InvokeMethod);

            if (invokeMethod == null)
                throw new Exception($"Expected {InvokeMethod} method on Action<,> but was not found.");

            var types =
                invokeMethod
                    .GetParameters()
                    .Select(o => o.ParameterType)
                    .ToArray();

            if (types.Length != 2)
                throw new Exception($"Expected Invoke method on setter to have 2 parameters, found {types.Length}");

            if (types[0] != primaryNode.GetType())
                throw new Exception(
                    $"Expected Invoke method on setter to have first parameter of type {primaryNode.GetType()}, found {types[0]}");

            // Invoke the delegate dynamically
            foreach (var relatedNodeDescendant in relatedNodeDescendants)
                try
                {
                    if (types[1] != relatedNodeDescendant.GetType())
                        throw new Exception(
                            $"Expected Invoke method on setter to have second parameter of type {relatedNodeDescendant.GetType()}, found {types[1]}");

                    object[] args = [primaryNode, relatedNodeDescendant];

                    invokeMethod
                        .Invoke(action, args);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Could not invoke setter on {sourceNavigation.DataType}", ex);
                }
        }
    }
}