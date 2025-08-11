using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using White.Knight.Domain;
using White.Knight.Domain.Exceptions;
using White.Knight.Interfaces;
using White.Knight.Interfaces.Command;
using White.Knight.Neo4J.Options;
using White.Knight.Neo4J.Translator;

namespace White.Knight.Neo4J
{
    public abstract class Neo4JKeylessRepositoryBase<TD>(
        Neo4JRepositoryFeatures<TD> repositoryFeatures) : IKeylessRepository<TD>
        where TD : new()
    {
        private readonly IClientSideEvaluationHandler _clientSideEvaluationHandler;
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
                        .Replace(Constants.NodeAliasPlaceholder, "a");

                var result =
                    await
                        _neo4JExecutor
                            .RunAsync(
                                translationResult.CommandText,
                                translationResult.Parameters,
                                cancellationToken
                            );

                return new RepositoryResult<TP>();
            }
            catch (UnparsableSpecificationException)
            {
                _clientSideEvaluationHandler
                    .Handle<TD>();

                //TODO: get all and client-side filter
                throw;/*
                var queryable =
                    await
                        _redisCache
                            .GetAllAsync(cancellationToken);

                return
                    await
                        queryable
                            .ApplyCommandQueryAsync(command);*/
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