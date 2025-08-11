using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.Neo4j;

namespace White.Knight.Neo4J.Tests.Integration
{
    public class TestContainerManager
    {
        private Neo4jContainer _neo4JContainer;

        public async Task StartAsync(int hostedPort)
        {
            _neo4JContainer =
                GetBuilder(hostedPort)
                    .Build();

            await
                _neo4JContainer
                    .StartAsync();
        }

        public async Task StopAsync()
        {
            if (_neo4JContainer != null)
            {
                await
                    _neo4JContainer
                        .StopAsync();

                await
                    _neo4JContainer
                        .DisposeAsync();

                _neo4JContainer = null;
            }
        }

        private static Neo4jBuilder GetBuilder(int hostedPort)
        {
            return
                new Neo4jBuilder()
                    .WithImage("neo4j:5.4")
                    .WithName($"neo4j-test-harness-{Guid.NewGuid()}")
                    .WithPortBinding(hostedPort, 7687)
                    .WithWaitStrategy(
                        Wait
                            .ForUnixContainer()
                            .UntilPortIsAvailable(7687)
                    );
        }
    }
}