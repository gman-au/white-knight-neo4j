using System.Collections.Generic;
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
    public class Neo4JExecutor<TD> : INeo4JExecutor<TD>
    {
        private readonly INeo4JConnector _connector;
        private readonly ILogger<Neo4JExecutor<TD>> _logger;
        private readonly Neo4JRepositoryConfigurationOptions _options;

        public Neo4JExecutor(
            INeo4JConnector connector,
            IOptions<Neo4JRepositoryConfigurationOptions> optionsAccessor,
            ILoggerFactory loggerFactory = null)
        {
            _connector = connector;
            _options = optionsAccessor.Value;
            _logger =
                (loggerFactory ?? new NullLoggerFactory())
                .CreateLogger<Neo4JExecutor<TD>>();
        }

        public async Task<IReadOnlyList<IRecord>> RunAsync(
            string commandString,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            var dbName =
                _options?
                    .DbName ??
                throw new MissingConfigurationException("Neo4JRepositoryConfigurationOptions -> DbName");

            await using var driver =
                await
                    _connector
                        .GetDriverAsync(cancellationToken);

            _logger
                .LogDebug("Running Neo4J query: [{query}]", commandString);

            var result =
                await
                    driver
                        .ExecutableQuery(commandString)
                        .WithParameters(parameters)
                        .WithConfig(new QueryConfig(database: dbName))
                        .ExecuteAsync(cancellationToken);

            return
                result
                    .Result;
        }
    }
}