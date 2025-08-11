using White.Knight.Abstractions.Options;

namespace White.Knight.Neo4J.Options
{
    public class Neo4JRepositoryConfigurationOptions : RepositoryConfigurationOptions
    {
        public string FolderPath { get; set; }
    }
}