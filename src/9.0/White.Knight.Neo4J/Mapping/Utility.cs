using System;
using System.Collections.Generic;
using System.Linq;
using White.Knight.Neo4J.Relationships;

namespace White.Knight.Neo4J.Mapping
{
    public static class Utility
    {
        public static void InvokeDescendantSetters(
            this IRelationshipNavigation sourceNavigation,
            object primaryNode,
            IEnumerable<object> relatedNodeDescendants)
        {
                var navigationProperties =
                    sourceNavigation
                        .GetType()
                        .GetProperties();

                var setter =
                    navigationProperties
                        .FirstOrDefault(o => o.Name == "Setter");

                if (setter != null)
                {
                    var action = setter.GetValue(sourceNavigation);
                    var actionType = action?.GetType() ?? throw new Exception("Could not map");

                    if (actionType.IsGenericType && actionType.GetGenericTypeDefinition() == typeof(Action<,>))
                    {
                        var invokeMethod = actionType.GetMethod("Invoke");
                        // Invoke the delegate dynamically
                        foreach (var relatedNodeDescendant in relatedNodeDescendants)
                        {
                            object[] args = [primaryNode, relatedNodeDescendant];
                            invokeMethod.Invoke(action, args);
                        }

                        /*if (lambdaExpression.Parameters.Count == 2 && lambdaExpression.ReturnType == typeof(void))
                        {
                            var typedParameters = lambdaExpression.Parameters;
                            if (typedParameters[0].Type == primaryNavigation.DataType)
                                if (typedParameters[1].Type == secondaryNavigation.DataType)
                                {
                                    foreach (var objectToAdd in objectsToAdd)
                                    {
                                        var setterMethod =
                                            lambdaExpression
                                                .Compile()
                                                .DynamicInvoke(mappedNode, objectToAdd);
                                    }
                                }
                        }*/
                    }
                }
                    //
        }
    }
}