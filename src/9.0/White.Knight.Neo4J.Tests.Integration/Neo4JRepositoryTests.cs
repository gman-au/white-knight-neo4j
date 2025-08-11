using System.Reflection;
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
        : AbstractedRepositoryTests(new Neo4JRepositoryTestContext(helper))
    {
        private static readonly Assembly RepositoryAssembly =
            Assembly
                .GetAssembly(typeof(AddressRepository));

        private class Neo4JRepositoryTestContext : RepositoryTestContextBase, IRepositoryTestContext
        {
            public Neo4JRepositoryTestContext(ITestOutputHelper testOutputHelper)
            {
                // specify csv harness
                LoadTestConfiguration<Neo4JTestHarness>();

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
        }
    }
}