using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using White.Knight.Domain.Exceptions;
using White.Knight.Neo4J.Options;

namespace White.Knight.Neo4J
{
    public class Neo4JConnector : INeo4JConnector
    {
        private readonly Neo4JRepositoryConfigurationOptions _options;

        public Neo4JConnector(IOptions<Neo4JRepositoryConfigurationOptions> optionsAccessor)
        {
            _options = optionsAccessor.Value;
        }

        public async Task<IDriver> GetDriverAsync(CancellationToken cancellationToken)
        {
            var dbUri = _options?.DbUri ?? throw new MissingConfigurationException("Neo4JRepositoryConfigurationOptions -> DbUri");
            var dbUser = _options?.DbUser ?? throw new MissingConfigurationException("Neo4JRepositoryConfigurationOptions -> DbUser");
            var dbPassword = _options?.DbPassword ??
                             throw new MissingConfigurationException("Neo4JRepositoryConfigurationOptions -> DbPassword");

            var driver =
                GraphDatabase
                    .Driver(dbUri, AuthTokens.Basic(dbUser, dbPassword));

            await
                driver
                    .VerifyConnectivityAsync();

            return driver;
        }
    }
}