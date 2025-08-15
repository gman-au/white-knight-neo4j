using System.Collections.Generic;

namespace White.Knight.Neo4J.Translator
{
    public class Neo4JTranslationResult
    {
        public string QueryCommandText { get; set; }

        public string CountCommandText { get; set; }

        public string CountCommandIndex { get; set; }

        public IDictionary<string, string> Parameters { get; set; }

        public Dictionary<int, char> AliasDictionary { get; set; }
    }
}