using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using White.Knight.Abstractions.Extensions;
using White.Knight.Csv.Options;
using White.Knight.Domain;
using White.Knight.Interfaces;
using White.Knight.Interfaces.Command;

namespace White.Knight.Csv
{
    public abstract class CsvFileKeylessRepositoryBase<TD>(
        CsvRepositoryFeatures<TD> repositoryFeatures) : IKeylessRepository<TD>
        where TD : new()
    {
        private readonly ICsvLoader<TD> _csvLoader = repositoryFeatures.CsvLoader;
        private readonly IRepositoryExceptionRethrower _exceptionRethrower = repositoryFeatures.ExceptionRethrower;
        protected readonly ILogger Logger = repositoryFeatures.LoggerFactory.CreateLogger<CsvFileKeylessRepositoryBase<TD>>();
        protected readonly Stopwatch Stopwatch = new();

        public abstract Expression<Func<TD, object>> DefaultOrderBy();

        public async Task<RepositoryResult<TP>> QueryAsync<TP>(
            IQueryCommand<TD, TP> command,
            CancellationToken cancellationToken)
        {
            try
            {
                Logger
                    .LogDebug("Querying records of type [{type}]", typeof(TD).Name);

                Stopwatch
                    .Restart();

                var queryable =
                    await
                        _csvLoader
                            .ReadAsync(cancellationToken);

                var results =
                    await
                        queryable
                            .ApplyCommandQueryAsync(command);

                Logger
                    .LogDebug("Queried records of type [{type}] in {ms} ms", typeof(TD).Name, Stopwatch.ElapsedMilliseconds);

                return results;
            }
            catch (Exception e)
            {
                Logger
                    .LogError("Error querying records of type [{type}]: {error}", typeof(TD).Name, e.Message);

                throw RethrowRepositoryException(e);
            }
            finally
            {
                Stopwatch
                    .Stop();
            }
        }

        protected Exception RethrowRepositoryException(Exception exception)
        {
            return _exceptionRethrower != null
                ? _exceptionRethrower.Rethrow(exception)
                : exception;
        }
    }
}