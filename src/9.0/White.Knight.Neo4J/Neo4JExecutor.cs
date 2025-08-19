using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using White.Knight.Domain.Exceptions;
using White.Knight.Neo4J.Options;

namespace White.Knight.Neo4J
{
    public class Neo4JExecutor(
        INeo4JConnector connector,
        IOptions<Neo4JRepositoryConfigurationOptions> optionsAccessor
    )
        : INeo4JExecutor
    {
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