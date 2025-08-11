using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using White.Knight.Csv.Attribute;
using White.Knight.Csv.Options;
using White.Knight.Injection.Abstractions;

namespace White.Knight.Csv.Injection
{
	public static class ServiceCollectionExtension
	{
        public static IServiceCollection AddCsvRepositories(
            this IServiceCollection services, 
            IConfigurationRoot configuration)
        {
            services
                .Configure<CsvRepositoryConfigurationOptions>(
                    configuration
                        .GetSection(nameof(CsvRepositoryConfigurationOptions))
                );
            
            services
                .AddTransient(typeof(ICsvLoader<>), typeof(CsvLoader<>));

            return services;
        }

		public static IServiceCollection AddAttributedCsvRepositories(
			this IServiceCollection services,
			Assembly repositoryAssembly
		)
		{
			services
				.AddAttributedRepositories<IsCsvRepositoryAttribute>(repositoryAssembly);
            
			return services;
		}

		public static IServiceCollection AddCsvRepositoryFeatures(
            this IServiceCollection services,
            IConfigurationRoot configuration)
		{
			services
                .AddRepositoryFeatures<CsvRepositoryConfigurationOptions>(configuration)
				.AddScoped(typeof(CsvRepositoryFeatures<>), typeof(CsvRepositoryFeatures<>))
				.AddScoped(typeof(ICsvLoader<>), typeof(CsvLoader<>));

			return services;
		}
	}
}