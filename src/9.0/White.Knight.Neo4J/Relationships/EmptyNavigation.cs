using System;

namespace White.Knight.Neo4J.Relationships
{
    public class EmptyNavigation : IRelationshipNavigation
    {
        public Type DataType => null;

        public Type TargetType => null;

        public Relationship Relationship { get; set; } = null;


        public IRelationshipNavigation Next() => null;
    }
}