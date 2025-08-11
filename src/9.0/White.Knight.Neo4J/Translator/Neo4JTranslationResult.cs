using System.Collections.Generic;

namespace White.Knight.Neo4J.Translator
{
    public class Neo4JTranslationResult
    {
        public string CommandText { get; set; }

        public IDictionary<string, string> Parameters { get; set; }
    }
}