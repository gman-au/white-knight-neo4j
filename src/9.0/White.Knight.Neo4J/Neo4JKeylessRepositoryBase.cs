using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using White.Knight.Abstractions.Extensions;
using White.Knight.Domain;
using White.Knight.Domain.Exceptions;
using White.Knight.Interfaces;
using White.Knight.Interfaces.Command;
using White.Knight.Neo4J.Options;
using White.Knight.Neo4J.Translator;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace White.Knight.Neo4J
{
    public abstract class Neo4JKeylessRepositoryBase<TD>(
        Neo4JRepositoryFeatures<TD> repositoryFeatures) : IKeylessRepository<TD>
        where TD : new()
    {
        private readonly IClientSideEvaluationHandler _clientSideEvaluationHandler = repositoryFeatures.ClientSideEvaluationHandler;
        private readonly ICommandTranslator<TD, Neo4JTranslationResult> _commandTranslator = repositoryFeatures.CommandTranslator;
        private readonly IRepositoryExceptionRethrower _exceptionRethrower = repositoryFeatures.ExceptionRethrower;
        private readonly INeo4JExecutor<TD> _neo4JExecutor = repositoryFeatures.Neo4JExecutor;
        protected readonly ILogger Logger = repositoryFeatures.LoggerFactory.CreateLogger<Neo4JKeylessRepositoryBase<TD>>();
        protected readonly Stopwatch Stopwatch = new();

        public abstract Expression<Func<TD, object>> DefaultOrderBy();

        public async Task<RepositoryResult<TP>> QueryAsync<TP>(
            IQueryCommand<TD, TP> command,
            CancellationToken cancellationToken)
        {
            try
            {
                Logger
                    .LogDebug("Querying records of type [{type}]", typeof(TD).Name);

                Stopwatch
                    .Restart();

                var translationResult =
                    _commandTranslator
                        .Translate(command);

                if (translationResult == null)
                    throw new Exception("There was an error translating the Redis command.");

                translationResult.CommandText =
                    translationResult
                        .CommandText
                        .Replace(Constants.ActionCommandPlaceholder, "RETURN")
                        .Replace(Constants.NodeAliasPlaceholder, Constants.CommonNodeAlias);

                var neo4JRecords =
                    await
                        _neo4JExecutor
                            .GetResultsAsync(
                                translationResult.CommandText,
                                translationResult.Parameters,
                                cancellationToken
                            );

                var results =
                    neo4JRecords;

                return new RepositoryResult<TP>
                {
                    Records =
                        results
                            .Select(o =>
                                command
                                    .ProjectionOptions
                                    .Projection
                                    .Compile()
                                    .Invoke(o)
                            ),
                    Count =
                        neo4JRecords
                            .Count
                };
            }
            catch (UnparsableSpecificationException)
            {
                _clientSideEvaluationHandler
                    .Handle<TD>();

                var entityName =
                    typeof(TD)
                        .Name;

                var commandText = $"MATCH ({Constants.CommonNodeAlias}:{entityName}) RETURN {Constants.CommonNodeAlias}";
                var neo4JRecords =
                    (await
                        _neo4JExecutor
                            .GetResultsAsync(
                                commandText,
                                new Dictionary<string, string>(),
                                cancellationToken
                            ))
                    .AsQueryable();

                return
                    await
                        neo4JRecords
                            .ApplyCommandQueryAsync(command);
            }
            catch (Exception e)
            {
                Logger
                    .LogError("Error querying records of type [{type}]: {error}", typeof(TD).Name, e.Message);

                throw RethrowRepositoryException(e);
            }
            finally
            {
                Stopwatch
                    .Stop();
            }
        }

        protected Exception RethrowRepositoryException(Exception exception)
        {
            return _exceptionRethrower != null
                ? _exceptionRethrower.Rethrow(exception)
                : exception;
        }
    }
}