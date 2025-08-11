using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using White.Knight.Interfaces;
using White.Knight.Interfaces.Command;

namespace White.Knight.Neo4J.Translator
{
    public class Neo4JCommandTranslator<TD, TResponse>(
        ILoggerFactory loggerFactory = null
    )
        : ICommandTranslator<TD, Neo4JTranslationResult> where TD : new()
    {
        private readonly ILogger _logger =
            (loggerFactory ?? new NullLoggerFactory())
            .CreateLogger<Neo4JCommandTranslator<TD, TResponse>>();

        public Neo4JTranslationResult Translate(ISingleRecordCommand<TD> command)
        {
            var key =
                command
                    .Key;

            var entityName =
                typeof(TD)
                    .Name;

            var commandText = $"MATCH (a:{entityName} {{ {Constants.IdFieldPlaceholder}: $id }}) RETURN a";

            var parameters = new Dictionary<string, string>
            {
                { "id", key.ToString() }
            };

            _logger
                .LogDebug("Translated Query: [{query}]", commandText);

            return new Neo4JTranslationResult
            {
                CommandText = commandText,
                Parameters = parameters
            };
        }

        public Neo4JTranslationResult Translate<TP>(IQueryCommand<TD, TP> command)
        {
            return new Neo4JTranslationResult
            {
                CommandText = null,
                Parameters = null
            };
        }

        public Neo4JTranslationResult Translate(IUpdateCommand<TD> command)
        {
            var entityName =
                typeof(TD)
                    .Name;

            var commandText = $"MERGE (a:{entityName} {{ {Constants.SetterStringPlaceholder} }}) RETURN a";

            var parameters = new Dictionary<string, string>
            {
            };

            _logger
                .LogDebug("Translated Query: [{query}]", commandText);

            return new Neo4JTranslationResult
            {
                CommandText = commandText,
                Parameters = parameters
            };
        }
    }
}