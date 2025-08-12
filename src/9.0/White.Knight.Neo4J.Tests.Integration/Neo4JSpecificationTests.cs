using System.Reflection;
using White.Knight.Neo4J.Injection;
using White.Knight.Neo4J.Tests.Integration.Repositories;
using White.Knight.Neo4J.Translator;
using White.Knight.Tests.Abstractions;
using White.Knight.Tests.Abstractions.Extensions;
using White.Knight.Tests.Abstractions.Spec;
using White.Knight.Tests.Abstractions.Tests;
using White.Knight.Tests.Domain.Specifications;
using Xunit.Abstractions;

namespace White.Knight.Neo4J.Tests.Integration
{
    public class Neo4JSpecificationTests(ITestOutputHelper helper)
        : AbstractedSpecificationTests(new InMemorySpecificationTestContext(helper))
    {
        private static readonly Assembly SpecAssembly =
            Assembly
                .GetAssembly(typeof(CustomerSpecByCustomerName));

        private static readonly Assembly RepositoryAssembly =
            Assembly
                .GetAssembly(typeof(AddressRepository));

        private class InMemorySpecificationTestContext : SpecificationTestContextBase<Neo4JTranslationResult>, ISpecificationTestContext
        {
            public InMemorySpecificationTestContext(ITestOutputHelper testOutputHelper)
            {
                SpecificationAssembly = SpecAssembly;

                // specify in memory harness
                LoadTestConfiguration();

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