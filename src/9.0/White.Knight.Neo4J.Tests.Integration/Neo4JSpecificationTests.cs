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
        : AbstractedSpecificationTests(new Neo4JSpecificationTestContext(helper))
    {
        private static readonly Assembly SpecAssembly =
            Assembly
                .GetAssembly(typeof(CustomerSpecByCustomerName));

        private static readonly Assembly RepositoryAssembly =
            Assembly
                .GetAssembly(typeof(AddressRepository));

        [Fact]
        public override void Throws_untransmutable_specification()
        {
            var context = (Neo4JSpecificationTestContext)GetContext();

            context
                .ActVerifyUntransmutableSpec();

            context
                .AssertClientSideEvaluationWasForced();
        }

        [Fact]
        public override void Throws_untransmutable_nested_specification()
        {
            var context = (Neo4JSpecificationTestContext)GetContext();

            context
                .ActVerifyNestedUntransmutableSpec();

            context
                .AssertClientSideEvaluationWasForced();
        }

        private class Neo4JSpecificationTestContext : SpecificationTestContextBase<Neo4JTranslationResult>, ISpecificationTestContext
        {
            public Neo4JSpecificationTestContext(ITestOutputHelper testOutputHelper)
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

            public void AssertClientSideEvaluationWasForced()
            {
                var result = Result as Neo4JTranslationResult;

                Assert.NotNull(result);
                Assert.True(result.ForcedClientSideEvaluation);
            }
        }
    }
}