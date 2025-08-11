using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace White.Knight.Neo4J
{
    public interface INeo4JExecutor<TD>
    {
        Task<IReadOnlyList<TD>> GetResultsAsync(
            string commandString,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken);

        Task RunCommandAsync(
            string commandString,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken);
    }
}