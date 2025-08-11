using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using White.Knight.Interfaces;
using White.Knight.Neo4J.Translator;

namespace White.Knight.Neo4J.Options
{
    public class Neo4JRepositoryFeatures<T>(
        INeo4JExecutor<T> neo4JExecutor,
        ICommandTranslator<T, Neo4JTranslationResult> commandTranslator,
        IClientSideEvaluationHandler clientSideEvaluationHandler,
        IRepositoryExceptionRethrower exceptionRethrower = null,
        ILoggerFactory loggerFactory = null)
        : INeo4JRepositoryFeatures<T>
    {
        public INeo4JExecutor<T> Neo4JExecutor { get; set; } = neo4JExecutor;

        public ICommandTranslator<T, Neo4JTranslationResult> CommandTranslator { get; set; } = commandTranslator;

        public IRepositoryExceptionRethrower ExceptionRethrower { get; set; } = exceptionRethrower;

        public IClientSideEvaluationHandler ClientSideEvaluationHandler { get; set; } = clientSideEvaluationHandler;

        public ILoggerFactory LoggerFactory { get; set; } = loggerFactory ?? new NullLoggerFactory();
    }
}