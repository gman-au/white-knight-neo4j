using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using White.Knight.Injection.Abstractions;
using White.Knight.Interfaces;
using White.Knight.Neo4J.Attribute;
using White.Knight.Neo4J.Mapping;
using White.Knight.Neo4J.Options;
using White.Knight.Neo4J.Translator;

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
                .AddTransient<INeo4JConnector, Neo4JConnector>();

            services
                .AddScoped(typeof(ICommandTranslator<,>), typeof(Neo4JCommandTranslator<,>))
                .AddScoped(typeof(INeo4JExecutor), typeof(Neo4JExecutor))
                .AddScoped(typeof(INodeMapper<>), typeof(NodeMapper<>));

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
                .AddScoped(typeof(INeo4JConnector), typeof(Neo4JConnector));

            return services;
        }
    }
}