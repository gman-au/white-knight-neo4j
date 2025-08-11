using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace White.Knight.Csv
{
    public interface ICsvLoader<T>
    {
        Task<IQueryable<T>> ReadAsync(CancellationToken cancellationToken);

        Task WriteAsync(IEnumerable<T> records, CancellationToken cancellationToken);
    }
}