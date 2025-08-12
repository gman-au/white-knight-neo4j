using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using White.Knight.Domain.Exceptions;
using White.Knight.Neo4J.Options;
using White.Knight.Neo4J.Translator;

namespace White.Knight.Neo4J
{
    public class Neo4JExecutor<TD>(
        INeo4JConnector connector,
        IOptions<Neo4JRepositoryConfigurationOptions> optionsAccessor,
        ILoggerFactory loggerFactory = null)
        : INeo4JExecutor<TD> where TD : new()
    {
        private readonly ILogger<Neo4JExecutor<TD>> _logger =
            (loggerFactory ?? new NullLoggerFactory())
            .CreateLogger<Neo4JExecutor<TD>>();

        private readonly Neo4JRepositoryConfigurationOptions _options = optionsAccessor.Value;

        public async Task<IReadOnlyList<TD>> GetResultsAsync(
            string commandString,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            await using var driver =
                await
                    connector
                        .GetDriverAsync(cancellationToken);

            var result =
                (await
                    BuildExecutableQueryAsync(
                        driver,
                        commandString,
                        parameters,
                        cancellationToken))
                .Result
                .Select(r => r[Constants.CommonNodeAlias].As<INode>());

            var mappedNodes =
                result
                    .Select(NodeMapper
                        .MapNode<TD>)
                    .ToList();

            return
                new ReadOnlyCollection<TD>(mappedNodes);
        }

        public async Task RunCommandAsync(
            string commandString,
            IDictionary<string, string> parameters,
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

        private static class NodeMapper
        {
            // public static T MapNode<T>(IRecord record) where T : new()
            public static T MapNode<T>(INode node) where T : new()
            {
                var obj = new T();
                var props = node.Properties;
                var type = typeof(T);

                foreach (var prop in type.GetProperties())
                    if (props.TryGetValue(prop.Name, out var value))
                    {
                        // Handle type conversion based on property type
                        if (prop.PropertyType == typeof(Guid))
                            prop.SetValue(obj, Guid.Parse(value.ToString()));
                        else if (prop.PropertyType == typeof(int))
                            prop.SetValue(obj, int.Parse(value.ToString()));
                        else if (prop.PropertyType == typeof(DateTime))
                            prop.SetValue(obj, DateTime.Parse(value.ToString()));
                        else
                            prop.SetValue(obj, value);
                    }

                // If property doesn't exist in node, leave as default
                return obj;
            }
        }
    }
}