using System.Collections.Generic;
using System.Linq;
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
        INeo4JExecutor<Customer> customerExecutor,
        INeo4JExecutor<Address> addressExecutor,
        INeo4JExecutor<Order> orderExecutor,
        IOptions<Neo4JRepositoryConfigurationOptions> optionsAccessor)
        : ITestHarness
    {
        private readonly Neo4JRepositoryConfigurationOptions _options = optionsAccessor.Value;

        public async Task<AbstractedRepositoryTestData> SetupRepositoryTestDataAsync()
        {
            var testData =
                testDataGenerator
                    .GenerateRepositoryTestData();

            // put 'records' into tables i.e. write to CSV files in advance of the tests
            await WriteRecordsAsync(testData.Addresses, addressExecutor);
            await WriteRecordsAsync(testData.Customers, customerExecutor);
            await WriteRecordsAsync(testData.Orders, orderExecutor);

            return testData;
        }

        private async Task WriteRecordsAsync<T>(IEnumerable<T> records, INeo4JExecutor<T> executor)
        {
            var entityName =
                typeof(T)
                    .Name;

            foreach (var record in records)
            {
                var commandMappings =
                    record
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

                await
                    executor
                        .RunAsync(
                            commandText,
                            parameters,
                            CancellationToken.None
                        );
            }
        }
    }
}