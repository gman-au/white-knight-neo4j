namespace White.Knight.Neo4J
{
    public class RelatedNodeBase<TParent, TChild>
    {
        public string RelationshipType { get; set; }

        public TParent Parent { get; set; }

        public TChild Child { get; set; }
    }
}