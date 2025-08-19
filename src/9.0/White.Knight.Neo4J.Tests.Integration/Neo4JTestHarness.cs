using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using White.Knight.Neo4J.Extensions;
using White.Knight.Neo4J.Options;
using White.Knight.Tests.Abstractions;
using White.Knight.Tests.Abstractions.Data;
using White.Knight.Tests.Domain;

namespace White.Knight.Neo4J.Tests.Integration
{
    public class Neo4JTestHarness(
        ITestDataGenerator testDataGenerator,
        INeo4JExecutor customerExecutor,
        INeo4JExecutor addressExecutor,
        INeo4JExecutor orderExecutor,
        IOptions<Neo4JRepositoryConfigurationOptions> optionsAccessor)
        : ITestHarness
    {
        private const bool WriteTestHarnessData = true;

        public async Task<AbstractedRepositoryTestData> SetupRepositoryTestDataAsync()
        {
            var testData =
                testDataGenerator
                    .GenerateRepositoryTestData();

            if (WriteTestHarnessData)
            {
                // put 'records' into tables i.e. write to Neo4j DB in advance of the tests
                await WriteNodesAsync(testData.Addresses, addressExecutor);
                await WriteNodesAsync(testData.Customers, customerExecutor);
                await WriteNodesAsync(testData.Orders, orderExecutor);

                await WriteRelationshipsAsync<Customer, Address>(
                    testData.Customers,
                    "CustomerId",
                    "CustomerId",
                    "LIVES_AT",
                    o => o.CustomerId,
                    customerExecutor,
                    "LIVED_IN_BY"
                );

                await WriteRelationshipsAsync<Customer, Order>(
                    testData.Customers,
                    "CustomerId",
                    "CustomerId",
                    "CREATED_ORDER",
                    o => o.CustomerId,
                    customerExecutor,
                    "CREATED_BY"
                );
            }

            return testData;
        }

        private async Task WriteNodesAsync<T>(IEnumerable<T> records, INeo4JExecutor executor)
        {
            var entityName =
                typeof(T)
                    .Name;

            foreach (var record in records)
            {
                var commandMappings =
                    record
                        .BuildNeo4JCommandMapping()
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

                await
                    executor
                        .RunCommandAsync(
                            parameters,
                            commandText,
                            CancellationToken.None
                        );
            }
        }

        private async Task WriteRelationshipsAsync<TParent, TChild>(
            IEnumerable<TParent> parents,
            string primaryKeyFieldName,
            string foreignKeyFieldName,
            string primaryRelationshipType,
            Expression<Func<TParent, Guid>> primaryKeyIdExpr,
            INeo4JExecutor executor,
            string secondaryRelationshipType = null)
        {
            var parentTypeName = typeof(TParent).Name;
            var childTypeName = typeof(TChild).Name;

            foreach (var parent in parents)
            {
                var primaryKeyValue =
                    primaryKeyIdExpr
                        .Compile()(parent);

                var commandString = $"MATCH (a:{parentTypeName} {{{primaryKeyFieldName}: '{primaryKeyValue}'}}), " +
                                    $"(b:{childTypeName} {{{foreignKeyFieldName}: '{primaryKeyValue}'}}) " +
                                    $"MERGE (a)-[:{primaryRelationshipType}]->(b) ";

                if (!string.IsNullOrWhiteSpace(secondaryRelationshipType))
                    commandString += $"MERGE (b)-[:{secondaryRelationshipType}]->(a)";

                await
                    executor
                        .RunCommandAsync(
                            new Dictionary<string, string>(),
                            commandString,
                            CancellationToken.None
                        );
            }
        }
    }
}