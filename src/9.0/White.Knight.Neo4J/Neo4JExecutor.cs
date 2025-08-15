using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using White.Knight.Domain.Exceptions;
using White.Knight.Neo4J.Options;

namespace White.Knight.Neo4J
{
    public class Neo4JExecutor(
        INeo4JConnector connector,
        IOptions<Neo4JRepositoryConfigurationOptions> optionsAccessor,
        ILoggerFactory loggerFactory = null)
        : INeo4JExecutor
    {
        private readonly ILogger<Neo4JExecutor> _logger =
            (loggerFactory ?? new NullLoggerFactory())
            .CreateLogger<Neo4JExecutor>();

        private readonly Neo4JRepositoryConfigurationOptions _options = optionsAccessor.Value;

        public async Task<Tuple<IReadOnlyList<IRecord>, long>> GetResultsAsync(
            IDictionary<string, string> parameters,
            string queryCommandString,
            string countCommandString,
            string countCommandIndex,
            CancellationToken cancellationToken)
        {
            await using var driver =
                await
                    connector
                        .GetDriverAsync(cancellationToken);

            var count = (long?)null;

            if (!string.IsNullOrWhiteSpace(countCommandString))
                count =
                    (await
                        BuildExecutableQueryAsync(
                            driver,
                            countCommandString,
                            new Dictionary<string, string>(),
                            cancellationToken))
                    .Result
                    .Select(r => r[countCommandIndex].As<long>())
                    .FirstOrDefault();

            // debug

            // queryCommandString = "MATCH (b:Customer)-[r:LIVES_AT|WORKS_AT]->(a:Address) RETURN a, b, r LIMIT 25";
            var debuggerCommandString = "MATCH (customer:Customer)-[r:CREATED_ORDER]->(order:Order) RETURN customer, order, r LIMIT 25";
            var debugger =
                (await
                    BuildExecutableQueryAsync(
                        driver,
                        debuggerCommandString,
                        parameters,
                        cancellationToken))
                .Result;

            /*var debuggerMappedNodes =
                nodeMapper
                    .Perform(debugger, graphStrategy);*/
            //

            var recordsList =
                (await
                    BuildExecutableQueryAsync(
                        driver,
                        queryCommandString,
                        parameters,
                        cancellationToken))
                .Result;

            count ??= recordsList.Count;

            return
                new Tuple<IReadOnlyList<IRecord>, long>(
                    recordsList,
                    count.Value
                );
        }

        public async Task RunCommandAsync(
            IDictionary<string, string> parameters,
            string commandString,
            CancellationToken cancellationToken)
        {
            await using var driver =
                await
                    connector
                        .GetDriverAsync(cancellationToken);

            await
                BuildExecutableQueryAsync(
                    driver,
                    commandString,
                    parameters,
                    cancellationToken);
        }

        private async Task<EagerResult<IReadOnlyList<IRecord>>> BuildExecutableQueryAsync(
            IDriver driver,
            string commandString,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            var dbName =
                _options?
                    .DbName ??
                throw new MissingConfigurationException("Neo4JRepositoryConfigurationOptions -> DbName");

            return
                await
                    driver
                        .ExecutableQuery(commandString)
                        .WithParameters(parameters)
                        .WithConfig(new QueryConfig(database: dbName))
                        .ExecuteAsync(cancellationToken);
        }
    }
}