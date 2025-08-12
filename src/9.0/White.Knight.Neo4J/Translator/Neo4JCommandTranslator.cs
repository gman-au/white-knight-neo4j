using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using White.Knight.Abstractions.Extensions;
using White.Knight.Domain;
using White.Knight.Domain.Exceptions;
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

            var commandText =
                $"MATCH ({Constants.NodeAliasPlaceholder}:{entityName} " +
                $"{{ {Constants.IdFieldPlaceholder}: $id }}) " +
                $"{Constants.ActionCommandPlaceholder} {Constants.NodeAliasPlaceholder}";

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
            var specification = command.Specification;
            var pagingOptions = command.PagingOptions;

            try
            {
                var entityName =
                    typeof(TD)
                        .Name;

                var query = Translate(specification);

                if (!string.IsNullOrEmpty(query))
                    query = $"WHERE {query}";

                var commandText =
                    $"MATCH ({Constants.NodeAliasPlaceholder}:{entityName}) " +
                    $"{query} " +
                    $"{Constants.PagingPlaceholder} " +
                    $"RETURN {Constants.NodeAliasPlaceholder} " +
                    $"{Constants.OrderByPlaceholder} ";

                var parameters = new Dictionary<string, string>();

                _logger
                    .LogDebug("Translated Query: ({specification}) [{query}]", specification.GetType().Name, query);

                var page = pagingOptions?.Page;
                var pageSize = pagingOptions?.PageSize;
                var sortDescending = pagingOptions?.Descending;
                var sort =
                    ClassEx
                        .ExtractPropertyInfo<TD>(pagingOptions?.OrderBy);

                var pagingString = string.Empty;
                var orderByString = string.Empty;
                if (pageSize.HasValue && page.HasValue) pagingString = $"LIMIT {pageSize.Value} SKIP {page.Value}";

                if (sort != null)
                    orderByString = $"ORDER BY {Constants.CommonNodeAlias}.{sort.Name} " +
                                    $"{(sortDescending.GetValueOrDefault() ? "DESC" : string.Empty)}";

                commandText =
                    commandText
                        .Replace(Constants.PagingPlaceholder, pagingString)
                        .Replace(Constants.OrderByPlaceholder, orderByString);

                return new Neo4JTranslationResult
                {
                    CommandText = commandText,
                    Parameters = parameters
                };
            }
            catch (Exception e) when (e is NotImplementedException or UnparsableSpecificationException)
            {
                _logger
                    .LogDebug("Error translating Query: ({specification})", specification.GetType().Name);

                throw;
            }
        }

        public Neo4JTranslationResult Translate(IUpdateCommand<TD> command)
        {
            var entityName =
                typeof(TD)
                    .Name;

            var commandText =
                $"MERGE ({Constants.NodeAliasPlaceholder}.:{entityName} " +
                $"{{ {Constants.SetterStringPlaceholder} }}) RETURN {Constants.NodeAliasPlaceholder}.";

            var parameters = new Dictionary<string, string>();

            _logger
                .LogDebug("Translated Query: [{query}]", commandText);

            return new Neo4JTranslationResult
            {
                CommandText = commandText,
                Parameters = parameters
            };
        }

        private string Translate(Specification<TD> spec)
        {
            var name = string.Empty;
            return spec switch
            {
                SpecificationByAll<TD> => "1=1",
                SpecificationByNone<TD> => "0=1",
                SpecificationByEquals<TD, string> eq =>
                    $"{Constants.NodeAliasPlaceholder}.{eq.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} = '{eq.Value}'",
                SpecificationByEquals<TD, int> eq =>
                    $"{Constants.NodeAliasPlaceholder}.{eq.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} = '{eq.Value}'",
                SpecificationByEquals<TD, Guid> eq =>
                    $"{Constants.NodeAliasPlaceholder}.{eq.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} = '{eq.Value.ToString()}'",
                SpecificationByAnd<TD> and => $"({Translate(and.Left)} AND {Translate(and.Right)})",
                SpecificationByOr<TD> and => $"({Translate(and.Left)} OR {Translate(and.Right)})",
                SpecificationByNot<TD> not => $"NOT ({Translate(not.Spec)})",
                SpecificationByTextStartsWith<TD> text =>
                    $"{Constants.NodeAliasPlaceholder}.{text.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} STARTS WITH '{text.Value}'",
                SpecificationByTextEndsWith<TD> text =>
                    $"{Constants.NodeAliasPlaceholder}.{text.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} ENDS WITH '{text.Value}'",
                SpecificationByTextContains<TD> text =>
                    $"{Constants.NodeAliasPlaceholder}.{text.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} CONTAINS '{text.Value}'",
                SpecificationThatIsNotCompatible<TD> => throw new UnparsableSpecificationException(),
                _ => throw new NotImplementedException()
            };
        }
    }
}