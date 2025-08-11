using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using White.Knight.Interfaces;

namespace White.Knight.Neo4J.Options
{
    public class Neo4JRepositoryFeatures<T>(
        ICsvLoader<T> csvLoader,
        IClientSideEvaluationHandler clientSideEvaluationHandler,
        IRepositoryExceptionRethrower exceptionRethrower = null,
        ILoggerFactory loggerFactory = null)
        : INeo4JRepositoryFeatures<T>
    {
        public ICsvLoader<T> CsvLoader { get; set; } = csvLoader;

        public IRepositoryExceptionRethrower ExceptionRethrower { get; set; } = exceptionRethrower;

        public IClientSideEvaluationHandler ClientSideEvaluationHandler { get; set; } = clientSideEvaluationHandler;

        public ILoggerFactory LoggerFactory { get; set; } = loggerFactory ?? new NullLoggerFactory();
    }
}