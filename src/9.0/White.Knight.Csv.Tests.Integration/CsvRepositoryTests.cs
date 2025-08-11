using System.Reflection;
using White.Knight.Csv.Injection;
using White.Knight.Csv.Tests.Integration.Repositories;
using White.Knight.Tests.Abstractions;
using White.Knight.Tests.Abstractions.Extensions;
using White.Knight.Tests.Abstractions.Repository;
using White.Knight.Tests.Abstractions.Tests;
using Xunit.Abstractions;

namespace White.Knight.Csv.Tests.Integration
{
    public class CsvRepositoryTests(ITestOutputHelper helper)
        : AbstractedRepositoryTests(new CsvRepositoryTestContext(helper))
    {
        private static readonly Assembly RepositoryAssembly =
            Assembly
                .GetAssembly(typeof(AddressRepository));

        private class CsvRepositoryTestContext : RepositoryTestContextBase, IRepositoryTestContext
        {
            public CsvRepositoryTestContext(ITestOutputHelper testOutputHelper)
            {
                // specify csv harness
                LoadTestConfiguration<CsvTestHarness>();

                // service initialisation
                ServiceCollection
                    .AddCsvRepositories(Configuration)
                    .AddAttributedCsvRepositories(RepositoryAssembly);

                // redirect ILogger output to Xunit console
                ServiceCollection
                    .ArrangeXunitOutputLogging(testOutputHelper);

                ServiceCollection
                    .AddCsvRepositoryFeatures(Configuration);

                LoadServiceProvider();
            }
        }
    }
}