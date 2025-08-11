using System;
using System.Linq.Expressions;
using White.Knight.Csv.Attribute;
using White.Knight.Csv.Options;
using White.Knight.Tests.Domain;

namespace White.Knight.Csv.Tests.Integration.Repositories
{
    [IsCsvRepository]
    public class AddressRepository(CsvRepositoryFeatures<Address> repositoryFeatures)
        : CsvFileKeylessRepositoryBase<Address>(repositoryFeatures)
    {
        public override Expression<Func<Address, object>> DefaultOrderBy() => o => o.AddressId;
    }
}