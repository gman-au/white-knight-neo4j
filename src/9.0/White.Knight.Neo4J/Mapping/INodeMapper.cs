using System.Collections.Generic;
using Neo4j.Driver;
using White.Knight.Neo4J.Navigations;

namespace White.Knight.Neo4J.Mapping
{
    public interface INodeMapper<TD>
    {
        public IEnumerable<TD> Perform(
            GraphStrategy<TD> graphStrategy,
            Dictionary<int, char> aliasDictionary,
            IEnumerable<IRecord> records);
    }
}