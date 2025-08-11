using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using White.Knight.Abstractions.Extensions;
using White.Knight.Abstractions.Fluent;
using White.Knight.Interfaces;
using White.Knight.Interfaces.Command;
using White.Knight.Neo4J.Extensions;
using White.Knight.Neo4J.Options;
using White.Knight.Neo4J.Translator;

namespace White.Knight.Neo4J
{
    public abstract class Neo4JRepositoryBase<TD>(
        Neo4JRepositoryFeatures<TD> repositoryFeatures)
        : Neo4JKeylessRepositoryBase<TD>(repositoryFeatures), IRepository<TD>
        where TD : new()
    {
        private readonly ICommandTranslator<TD, Neo4JTranslationResult> _commandTranslator = repositoryFeatures.CommandTranslator;
        private readonly INeo4JExecutor<TD> _neo4JExecutor = repositoryFeatures.Neo4JExecutor;

        public override Expression<Func<TD, object>> DefaultOrderBy()
        {
            return KeyExpression();
        }

        public abstract Expression<Func<TD, object>> KeyExpression();

        public virtual async Task<TD> SingleRecordAsync(object key, CancellationToken cancellationToken)
        {
            return await
                SingleRecordAsync(
                    key
                        .ToSingleRecordCommand<TD>(),
                    cancellationToken
                );
        }

        public async Task<TD> SingleRecordAsync(ISingleRecordCommand<TD> command, CancellationToken cancellationToken)
        {
            var key = command.Key;

            try
            {
                Logger
                    .LogDebug("Retrieving single record with key [{key}]", key);

                Stopwatch
                    .Restart();

                var translationResult =
                    _commandTranslator
                        .Translate(command);

                if (translationResult == null)
                    throw new Exception("There was an error translating the Neo4j command.");

                var idFieldName =
                    ClassEx
                        .ExtractPropertyInfo<TD>(KeyExpression())?
                        .Name ??
                    throw new Exception($"Could not retrieve key expression field from entity type {typeof(TD).Name}");

                translationResult.CommandText =
                    translationResult
                        .CommandText
                        .Replace(Constants.IdFieldPlaceholder, idFieldName)
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

                Logger
                    .LogDebug("Retrieved single record with key [{key}] in {ms} ms", key,
                        Stopwatch.ElapsedMilliseconds);

                //return csvEntity;
                return default;
            }
            catch (Exception e)
            {
                Logger
                    .LogError("Retrieving single record with key [{key}]: {error}", key, e.Message);

                throw RethrowRepositoryException(e);
            }
            finally
            {
                Stopwatch
                    .Stop();
            }
        }

        public virtual async Task<TD> AddOrUpdateAsync(
            IUpdateCommand<TD> command,
            CancellationToken cancellationToken = default)
        {
            return await AddOrUpdateWithModifiedAsync(
                command.Entity,
                command.Inclusions,
                command.Exclusions,
                cancellationToken
            );
        }

        public async Task<object> DeleteRecordAsync(ISingleRecordCommand<TD> command,
            CancellationToken cancellationToken)
        {
            var key = command.Key;

            try
            {
                Logger
                    .LogDebug("Deleting record with key [{key}]", key);

                Stopwatch
                    .Restart();

                var translationResult =
                    _commandTranslator
                        .Translate(command);

                if (translationResult == null)
                    throw new Exception("There was an error translating the Neo4j command.");

                var idFieldName =
                    ClassEx
                        .ExtractPropertyInfo<TD>(KeyExpression())?
                        .Name ??
                    throw new Exception($"Could not retrieve key expression field from entity type {typeof(TD).Name}");

                translationResult.CommandText =
                    translationResult
                        .CommandText
                        .Replace(Constants.IdFieldPlaceholder, idFieldName)
                        .Replace(Constants.ActionCommandPlaceholder, "DELETE")
                        .Replace(Constants.NodeAliasPlaceholder, "a");

                var result =
                    await
                        _neo4JExecutor
                            .RunAsync(
                                translationResult.CommandText,
                                translationResult.Parameters,
                                cancellationToken
                            );

                Logger
                    .LogDebug("Deleted record with key [{key}] in {ms} ms", key, Stopwatch.ElapsedMilliseconds);

                return key;
            }
            catch (Exception e)
            {
                Logger
                    .LogError("Error deleting record key [{key}]: {error}", key, e.Message);

                throw RethrowRepositoryException(e);
            }
            finally
            {
                Stopwatch
                    .Stop();
            }
        }

        private async Task<TD> AddOrUpdateWithModifiedAsync(
            TD sourceEntity,
            Expression<Func<TD, object>>[] fieldsToModify,
            Expression<Func<TD, object>>[] fieldsToPreserve,
            CancellationToken cancellationToken
        )
        {
            TD entityToCommit;

            try
            {
                Logger
                    .LogDebug("Upserting record of type [{type}]", typeof(TD).Name);

                Stopwatch
                    .Restart();

                entityToCommit =
                    sourceEntity;

                var entityName =
                    typeof(TD)
                        .Name;

                var commandMappings =
                    entityToCommit
                        .BuildNeo4jCommandMapping()
                        .ToList();

                var commandParameterString =
                    string
                        .Join(
                            ", ",
                            commandMappings
                                .Select(
                                    o => $"{o.Item2}: ${o.Item1}")
                        );

                var commandText = $"MERGE (a:{entityName} {{ {commandParameterString} }}) RETURN a";

                var parameters =
                    commandMappings
                        .ToDictionary(o => o.Item1, o => o.Item3);

                var result =
                    await
                        _neo4JExecutor
                            .RunAsync(
                                commandText,
                                parameters,
                                cancellationToken
                            );

                Logger
                    .LogDebug(
                        "Upserted {count} records of type [{type}] in {ms} ms",
                        result.Count,
                        typeof(TD).Name,
                        Stopwatch.ElapsedMilliseconds
                    );
            }
            catch (Exception e)
            {
                Logger
                    .LogError("Error upserting record of type [{type}]: {error}", typeof(TD).Name, e.Message);

                throw RethrowRepositoryException(e);
            }
            finally
            {
                Stopwatch
                    .Stop();
            }


            return entityToCommit;
        }
    }
}