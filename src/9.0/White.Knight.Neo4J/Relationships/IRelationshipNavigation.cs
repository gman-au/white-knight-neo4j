using System;

namespace White.Knight.Neo4J.Relationships
{
    public interface IRelationshipNavigation
    {
        public Type DataType { get; }

        public Relationship Relationship { get; set; }

        public IRelationshipNavigation Next();
    }
}