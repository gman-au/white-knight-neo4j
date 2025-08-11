using White.Knight.Interfaces;

namespace White.Knight.Neo4J.Options
{
    public interface INeo4JRepositoryFeatures<T> : IRepositoryFeatures
    {
        public ICsvLoader<T> CsvLoader { get; set; }
    }
}