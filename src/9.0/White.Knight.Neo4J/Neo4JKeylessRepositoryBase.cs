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
using White.Knight.Neo4J.Mapping;
using White.Knight.Neo4J.Navigations;
using White.Knight.Neo4J.Options;
using White.Knight.Neo4J.Relationships;
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
        private readonly INeo4JExecutor _neo4JExecutor = repositoryFeatures.Neo4JExecutor;
        private readonly INodeMapper<TD> _nodeMapper = repositoryFeatures.NodeMapper;
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

                command.NavigationStrategy ??= new GraphStrategy<TD>(RelationshipNavigation.Empty);

                var translationResult =
                    _commandTranslator
                        .Translate(command);

                if (translationResult == null)
                    throw new Exception("There was an error translating the Redis command.");

                translationResult.QueryCommandText =
                    translationResult
                        .QueryCommandText
                        .Replace(Constants.ActionCommandPlaceholder, "RETURN");

                translationResult.CountCommandText =
                    translationResult
                        .CountCommandText
                        .Replace(Constants.NodeAliasPlaceholder, Constants.CommonNodeAlias);

                var (records, recordCount) =
                    await
                        _neo4JExecutor
                            .GetResultsAsync(
                                translationResult.Parameters,
                                translationResult.QueryCommandText,
                                translationResult.CountCommandText,
                                translationResult.CountCommandIndex,
                                cancellationToken
                            );

                var mappedRecords =
                    _nodeMapper
                        .Perform(
                            command.NavigationStrategy as GraphStrategy<TD>,
                            translationResult.AliasDictionary,
                            records.ToArray()
                        );

                return new RepositoryResult<TP>
                {
                    Records =
                        mappedRecords
                            .Select(o =>
                                command
                                    .ProjectionOptions
                                    .Projection
                                    .Compile()
                                    .Invoke(o)
                            ),
                    Count = recordCount
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
                var records =
                    (await
                        _neo4JExecutor
                            .GetResultsAsync(
                                new Dictionary<string, string>(),
                                commandText,
                                null,
                                null,
                                cancellationToken
                            ))
                    .Item1;

                // TODO: send proper dictionary
                var mappedRecords =
                    _nodeMapper
                        .Perform(
                            command.NavigationStrategy as GraphStrategy<TD>,
                            new Dictionary<int, char>(),
                            records.ToArray())
                        .AsQueryable();

                return
                    await
                        mappedRecords
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