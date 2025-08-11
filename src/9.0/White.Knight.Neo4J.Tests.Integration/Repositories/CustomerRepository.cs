using System;
using System.Linq.Expressions;
using White.Knight.Neo4J.Attribute;
using White.Knight.Neo4J.Options;
using White.Knight.Tests.Domain;

namespace White.Knight.Neo4J.Tests.Integration.Repositories
{
    [IsNeo4JRepository]
    public class CustomerRepository(Neo4JRepositoryFeatures<Customer> repositoryFeatures)
        : Neo4JRepositoryBase<Customer>(repositoryFeatures)
    {
        public override Expression<Func<Customer, object>> KeyExpression()
        {
            return b => b.CustomerId;
        }
    }
}