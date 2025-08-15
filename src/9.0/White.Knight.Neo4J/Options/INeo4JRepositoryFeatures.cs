using White.Knight.Interfaces;
using White.Knight.Neo4J.Translator;

namespace White.Knight.Neo4J.Options
{
    public interface INeo4JRepositoryFeatures<T> : IRepositoryFeatures
    {
        public INeo4JExecutor Neo4JExecutor { get; set; }

        public ICommandTranslator<T, Neo4JTranslationResult> CommandTranslator { get; set; }
    }
}