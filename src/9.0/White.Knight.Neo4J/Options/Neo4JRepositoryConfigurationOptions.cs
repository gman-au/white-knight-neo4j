using White.Knight.Abstractions.Options;

namespace White.Knight.Neo4J.Options
{
    public class Neo4JRepositoryConfigurationOptions : RepositoryConfigurationOptions
    {
        public string DbName { get; set; }

        public string DbUri { get; set; }

        public string DbUser { get; set; }

        public string DbPassword { get; set; }
    }
}