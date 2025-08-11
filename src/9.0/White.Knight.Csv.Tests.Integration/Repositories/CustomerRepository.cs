using System;
using System.Linq.Expressions;
using White.Knight.Csv.Attribute;
using White.Knight.Csv.Options;
using White.Knight.Tests.Domain;

namespace White.Knight.Csv.Tests.Integration.Repositories
{
    [IsCsvRepository]
    public class CustomerRepository(CsvRepositoryFeatures<Customer> repositoryFeatures)
        : CsvFileRepositoryBase<Customer>(repositoryFeatures)
    {
        public override Expression<Func<Customer, object>> KeyExpression() => b => b.CustomerId;
    }
}