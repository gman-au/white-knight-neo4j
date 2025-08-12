using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace White.Knight.Neo4J
{
    public interface INeo4JExecutor<TD>
    {
        Task<Tuple<IReadOnlyList<TD>, long>> GetResultsAsync(
            IDictionary<string, string> parameters,
            string queryCommandString,
            string countCommandString,
            CancellationToken cancellationToken
        );

        Task RunCommandAsync(
            string commandString,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken
        );
    }
}