using System;
using System.Collections.Generic;
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
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetPaths()
        {
            throw new NotImplementedException();
        }
    }
}