using System;
using System.Linq;
using System.Linq.Expressions;
using White.Knight.Interfaces;
using White.Knight.Neo4J.Relationships;

namespace White.Knight.Neo4J.Navigations
{
    public class GraphStrategy<T>(IRelationshipNavigation relationshipNavigation) : INavigationStrategy<T>
    {
        public readonly IRelationshipNavigation RelationshipNavigation = relationshipNavigation;

        public Expression<Func<IQueryable<T>, IQueryable<T>>> GetStrategy()
        {
            // TODO: translate IRelationshipNavigation into IQueryable expression
            return o => o;
        }
    }
}