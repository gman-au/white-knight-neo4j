using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace White.Knight.Neo4J
{
    public interface INeo4JConnector
    {
        Task<IDriver> GetDriverAsync(CancellationToken cancellationToken);
    }
}