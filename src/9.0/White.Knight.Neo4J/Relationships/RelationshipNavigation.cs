using System;
using System.Collections.Generic;
using System.Linq;

namespace White.Knight.Neo4J.Relationships
{
    public class RelationshipNavigationBase<T1>(string type) : IRelationshipNavigation
    {
        public Type DataType { get; } = typeof(T1);

        public Relationship Relationship { get; set; } = new(type);

        public virtual IRelationshipNavigation Next() => ChainedRelationship;

        public virtual IRelationshipNavigation ChainedRelationship { get; set; }
    }

    public class RelationshipNavigation<T1, T2, T3, T4, T5>(
        string type,
        Action<T1, T2> setter,
        RelationshipNavigation<T2, T3, T4, T5> relationship) : RelationshipNavigationBase<T1>(type)
    {
        public override IRelationshipNavigation ChainedRelationship { get; set; } = relationship;

        public Action<T1, T2> Setter { get; set; } = setter;
    }

    public class RelationshipNavigation<T1, T2, T3, T4>(
        string type,
        Action<T1, T2> setter,
        RelationshipNavigation<T2, T3, T4> relationship) : RelationshipNavigationBase<T1>(type)
    {
        public override IRelationshipNavigation ChainedRelationship { get; set; } = relationship;

        public Action<T1, T2> Setter { get; set; } = setter;
    }

    public class RelationshipNavigation<T1, T2, T3>(
        string type,
        Action<T1, T2> setter,
        RelationshipNavigation<T2, T3> relationship) : RelationshipNavigationBase<T1>(type)
    {
        public override IRelationshipNavigation ChainedRelationship { get; set; } = relationship;

        public Action<T1, T2> Setter { get; set; } = setter;
    }

    public class RelationshipNavigation<T1, T2>(
        string type,
        Action<T1, T2> setter)
        : RelationshipNavigationBase<T1>(type)
    {
        public override IRelationshipNavigation ChainedRelationship { get; set; } = new RelationshipNavigation<T2>();

        public Action<T1, T2> Setter { get; set; } = setter;
    }

    public class RelationshipNavigation<T1>() : RelationshipNavigationBase<T1>(null)
    {
        public override IRelationshipNavigation ChainedRelationship { get; set; } = RelationshipNavigation.Empty;
    }

    public static class RelationshipNavigation
    {
        public static readonly IRelationshipNavigation Empty = new EmptyNavigation();

        public static IEnumerable<IRelationshipNavigation> GetNavigationChain(
            this IRelationshipNavigation start,
            bool reverse = false)
        {
            var current = start;
            var sequence = new List<IRelationshipNavigation>();

            while (current != null && current != Empty)
            {
                sequence.Add(current);
                current = current.Next();
            }

            return reverse ? sequence.AsEnumerable().Reverse() : sequence;
        }
    }
}