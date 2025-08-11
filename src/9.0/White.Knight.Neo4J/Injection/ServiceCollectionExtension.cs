using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using White.Knight.Injection.Abstractions;
using White.Knight.Neo4J.Attribute;
using White.Knight.Neo4J.Options;

namespace White.Knight.Neo4J.Injection
{
	public static class ServiceCollectionExtension
	{
        public static IServiceCollection AddNeo4JRepositories(
            this IServiceCollection services, 
            IConfigurationRoot configuration)
        {
            services
                .Configure<Neo4JRepositoryConfigurationOptions>(
                    configuration
                        .GetSection(nameof(Neo4JRepositoryConfigurationOptions))
                );
            
            services
                .AddTransient(typeof(ICsvLoader<>), typeof(CsvLoader<>));

            return services;
        }

		public static IServiceCollection AddAttributedNeo4JRepositories(
			this IServiceCollection services,
			Assembly repositoryAssembly
		)
		{
			services
				.AddAttributedRepositories<IsNeo4JRepositoryAttribute>(repositoryAssembly);
            
			return services;
		}

		public static IServiceCollection AddNeo4JRepositoryFeatures(
            this IServiceCollection services,
            IConfigurationRoot configuration)
		{
			services
                .AddRepositoryFeatures<Neo4JRepositoryConfigurationOptions>(configuration)
				.AddScoped(typeof(Neo4JRepositoryFeatures<>), typeof(Neo4JRepositoryFeatures<>))
				.AddScoped(typeof(ICsvLoader<>), typeof(CsvLoader<>));

			return services;
		}
	}
}