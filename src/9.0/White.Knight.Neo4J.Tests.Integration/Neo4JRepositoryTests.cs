using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using White.Knight.Neo4J.Injection;
using White.Knight.Neo4J.Tests.Integration.Repositories;
using White.Knight.Tests.Abstractions;
using White.Knight.Tests.Abstractions.Extensions;
using White.Knight.Tests.Abstractions.Repository;
using White.Knight.Tests.Abstractions.Tests;
using Xunit.Abstractions;

namespace White.Knight.Neo4J.Tests.Integration
{
    public class Neo4JRepositoryTests(ITestOutputHelper helper)
        : AbstractedRepositoryTests(new Neo4JRepositoryTestContext(helper)), IAsyncLifetime
    {
        private static readonly Assembly RepositoryAssembly =
            Assembly
                .GetAssembly(typeof(AddressRepository));

        private readonly TestContainerManager _testContainerManager = new();

        public async Task InitializeAsync()
        {
            var context = GetContext() as Neo4JRepositoryTestContext;

            await
                _testContainerManager
                    .StartAsync(context.GetHostedPort());
        }

        public async Task DisposeAsync()
        {
            await
                _testContainerManager
                    .StopAsync();
        }

        public override Task Test_Search_By_Sub_Item_Query_Id()
        {
            // N/A. Sub query item searches are not invalid, but the nodes would likely be decoupled in Neo4j
            return
                Task
                    .CompletedTask;
        }

        private class Neo4JRepositoryTestContext : RepositoryTestContextBase, IRepositoryTestContext
        {
            private readonly int _hostedPort;

            public Neo4JRepositoryTestContext(ITestOutputHelper testOutputHelper)
            {
                _hostedPort =
                    new Random()
                        .Next(10000, 11000);

                // specify csv harness
                LoadTestConfiguration<Neo4JTestHarness>();

                Configuration =
                    InterceptConfiguration(Configuration, _hostedPort);

                // service initialisation
                ServiceCollection
                    .AddNeo4JRepositories(Configuration)
                    .AddAttributedNeo4JRepositories(RepositoryAssembly);

                // redirect ILogger output to Xunit console
                ServiceCollection
                    .ArrangeXunitOutputLogging(testOutputHelper);

                ServiceCollection
                    .AddNeo4JRepositoryFeatures(Configuration);

                LoadServiceProvider();
            }

            public int GetHostedPort()
            {
                return _hostedPort;
            }

            private static IConfigurationRoot InterceptConfiguration(IConfigurationRoot existingConfiguration, int hostedPort)
            {
                var inMemoryCollection = new Dictionary<string, string>
                {
                    ["Neo4JRepositoryConfigurationOptions:DbUri"] = $"neo4j://localhost:{hostedPort}"
                };

                // Add the in-memory collection to the configuration
                return new ConfigurationBuilder()
                    .AddConfiguration(existingConfiguration)
                    .AddInMemoryCollection(inMemoryCollection)
                    .Build();
            }
        }
    }
}