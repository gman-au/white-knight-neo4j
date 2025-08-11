using System;
using System.Linq.Expressions;
using White.Knight.Neo4J.Attribute;
using White.Knight.Neo4J.Options;
using White.Knight.Tests.Domain;

namespace White.Knight.Neo4J.Tests.Integration.Repositories
{
    [IsNeo4JRepository]
    public class AddressRepository(Neo4JRepositoryFeatures<Address> repositoryFeatures)
        : Neo4JKeylessRepositoryBase<Address>(repositoryFeatures)
    {
        public override Expression<Func<Address, object>> DefaultOrderBy() => o => o.AddressId;
    }
}