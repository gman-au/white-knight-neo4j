using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace White.Knight.Neo4J
{
    public interface INeo4JExecutor
    {
        Task<Tuple<IReadOnlyList<IRecord>, long>> GetResultsAsync(
            IDictionary<string, string> parameters,
            string queryCommandString,
            string countCommandString,
            string countCommandIndex,
            CancellationToken cancellationToken);

        Task RunCommandAsync(
            IDictionary<string, string> parameters,
            string commandString,
            CancellationToken cancellationToken
        );
    }
}