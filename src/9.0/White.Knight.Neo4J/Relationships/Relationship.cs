namespace White.Knight.Neo4J.Relationships
{
    public class Relationship(string type)
    {
        public string Type { get; set; } = type;
    }
}