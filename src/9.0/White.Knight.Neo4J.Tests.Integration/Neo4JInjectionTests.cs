using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using White.Knight.Domain.Enum;
using White.Knight.Injection.Abstractions;
using White.Knight.Neo4J.Injection;
using White.Knight.Neo4J.Options;
using White.Knight.Neo4J.Tests.Integration.Repositories;
using White.Knight.Tests.Abstractions;
using White.Knight.Tests.Abstractions.Extensions;
using White.Knight.Tests.Abstractions.Injection;
using White.Knight.Tests.Abstractions.Tests;
using White.Knight.Tests.Domain;

namespace White.Knight.Neo4J.Tests.Integration
{
    public class Neo4JInjectionTests() : AbstractedInjectionTests(new CsvInjectionTestContext())
    {
        private static readonly Assembly RepositoryAssembly =
            Assembly
                .GetAssembly(typeof(AddressRepository));

        private class CsvInjectionTestContext : InjectionTestContextBase, IInjectionTestContext
        {
            public override void ArrangeImplementedServices()
            {
                ServiceCollection
                    .AddNeo4JRepositories(Configuration)
                    .AddAttributedNeo4JRepositories(RepositoryAssembly);

                ServiceCollection
                    .AddRepositoryFeatures<Neo4JRepositoryConfigurationOptions>(Configuration)
                    .AddNeo4JRepositoryFeatures(Configuration);
            }

            public override void ArrangeDefinedClientSideConfiguration()
            {
                Configuration =
                    Configuration
                        .ArrangeThrowOnClientSideEvaluation<Neo4JRepositoryConfigurationOptions>();
            }

            public override void AssertLoggerFactoryResolved()
            {
                var features =
                    Sut
                        .GetRequiredService<Neo4JRepositoryFeatures<Address>>();

                Assert
                    .NotNull(features);

                var loggerFactory =
                    features
                        .LoggerFactory;

                Assert
                    .NotNull(loggerFactory);
            }

            public override void AssertRepositoryOptionsResolvedWithDefault()
            {
                var options =
                    Sut
                        .GetRequiredService<IOptions<Neo4JRepositoryConfigurationOptions>>();

                Assert.NotNull(options.Value);

                Assert.Equal(ClientSideEvaluationResponseTypeEnum.Warn, options.Value.ClientSideEvaluationResponse);
            }

            public override void AssertRepositoryOptionsResolvedWithDefined()
            {
                var options =
                    Sut
                        .GetRequiredService<IOptions<Neo4JRepositoryConfigurationOptions>>();

                Assert.NotNull(options.Value);

                Assert.Equal(ClientSideEvaluationResponseTypeEnum.Throw, options.Value.ClientSideEvaluationResponse);
            }
        }
    }
}