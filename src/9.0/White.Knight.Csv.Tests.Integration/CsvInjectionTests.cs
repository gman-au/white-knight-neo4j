using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using White.Knight.Csv.Injection;
using White.Knight.Csv.Options;
using White.Knight.Csv.Tests.Integration.Repositories;
using White.Knight.Domain.Enum;
using White.Knight.Injection.Abstractions;
using White.Knight.Tests.Abstractions;
using White.Knight.Tests.Abstractions.Extensions;
using White.Knight.Tests.Abstractions.Injection;
using White.Knight.Tests.Abstractions.Tests;
using White.Knight.Tests.Domain;

namespace White.Knight.Csv.Tests.Integration
{
    public class CsvInjectionTests() : AbstractedInjectionTests(new CsvInjectionTestContext())
    {
        private static readonly Assembly RepositoryAssembly =
            Assembly
                .GetAssembly(typeof(AddressRepository));

        private class CsvInjectionTestContext : InjectionTestContextBase, IInjectionTestContext
        {
            public override void ArrangeImplementedServices()
            {
                ServiceCollection
                    .AddCsvRepositories(Configuration)
                    .AddAttributedCsvRepositories(RepositoryAssembly);

                ServiceCollection
                    .AddRepositoryFeatures<CsvRepositoryConfigurationOptions>(Configuration)
                    .AddCsvRepositoryFeatures(Configuration);
            }

            public override void ArrangeDefinedClientSideConfiguration()
            {
                Configuration =
                    Configuration
                        .ArrangeThrowOnClientSideEvaluation<CsvRepositoryConfigurationOptions>();
            }

            public override void AssertLoggerFactoryResolved()
            {
                var features =
                    Sut
                        .GetRequiredService<CsvRepositoryFeatures<Address>>();

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
                        .GetRequiredService<IOptions<CsvRepositoryConfigurationOptions>>();

                Assert.NotNull(options.Value);

                Assert.Equal(ClientSideEvaluationResponseTypeEnum.Warn, options.Value.ClientSideEvaluationResponse);
            }

            public override void AssertRepositoryOptionsResolvedWithDefined()
            {
                var options =
                    Sut
                        .GetRequiredService<IOptions<CsvRepositoryConfigurationOptions>>();

                Assert.NotNull(options.Value);

                Assert.Equal(ClientSideEvaluationResponseTypeEnum.Throw, options.Value.ClientSideEvaluationResponse);
            }
        }
    }
}