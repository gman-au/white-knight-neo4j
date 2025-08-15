using White.Knight.Interfaces.Command;
using White.Knight.Neo4J.Navigations;
using White.Knight.Neo4J.Relationships;

namespace White.Knight.Neo4J.Extensions
{
    public static class FluentCommandEx
    {
        public static IQueryCommand<TSource, TSource> WithRelationshipStrategy<TSource>(
            this IQueryCommand<TSource, TSource> command,
            IRelationshipNavigation navigation)
            where TSource : new()
        {
            command.NavigationStrategy = new GraphStrategy<TSource>(navigation);
            return command;
        }
    }
}