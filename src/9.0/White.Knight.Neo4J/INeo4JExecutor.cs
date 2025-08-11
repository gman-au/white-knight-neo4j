using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace White.Knight.Neo4J
{
    public interface INeo4JExecutor<TD>
    {
        Task<IReadOnlyList<IRecord>> RunAsync(
            string commandString,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken);
    }
}