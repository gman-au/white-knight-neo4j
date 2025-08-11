using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Extensions.Options;
using White.Knight.Csv.Options;
using White.Knight.Domain.Exceptions;

namespace White.Knight.Csv
{
    public class CsvLoader<TD>(IOptions<CsvRepositoryConfigurationOptions> optionsAccessor) : ICsvLoader<TD>
    {
        private readonly string _fileName =
            $"{typeof(TD).Name}.csv"
                .ToLowerInvariant();

        private readonly string _folderPath =
            optionsAccessor?.Value
                .FolderPath ??
            throw new MissingConfigurationException("CsvRepositoryConfigurationOptions -> FolderPath");

        public async Task<IQueryable<TD>> ReadAsync(CancellationToken cancellationToken)
        {
            var filePath =
                Path
                    .Combine(_folderPath, _fileName);

            using var reader = new StreamReader(filePath);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records =
                csvReader
                    .GetRecords<TD>()
                    .ToList();

            return
                records
                    .AsQueryable();
        }

        public async Task WriteAsync(IEnumerable<TD> records, CancellationToken cancellationToken)
        {
            var filePath =
                Path
                    .Combine(_folderPath, _fileName);

            await using var reader = new StreamWriter(filePath);
            await using var csvReader = new CsvWriter(reader, CultureInfo.InvariantCulture);

            await
                csvReader
                    .WriteRecordsAsync(records, cancellationToken);
        }
    }
}