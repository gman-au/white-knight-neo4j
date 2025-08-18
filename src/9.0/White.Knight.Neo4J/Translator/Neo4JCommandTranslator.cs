using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using White.Knight.Abstractions.Extensions;
using White.Knight.Domain;
using White.Knight.Domain.Exceptions;
using White.Knight.Interfaces;
using White.Knight.Interfaces.Command;
using White.Knight.Neo4J.Navigations;
using White.Knight.Neo4J.Relationships;

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
            var graphStrategy =
                command.NavigationStrategy as GraphStrategy<TD> ??
                new GraphStrategy<TD>(new RelationshipNavigation<TD>());

            var key =
                command
                    .Key;

            var aliasDictionary = BuildAliasDictionary(graphStrategy);

            var (matchString, returnAliases) = BuildMatchString(graphStrategy, aliasDictionary);

            matchString =
                matchString
                    .Replace(Constants.IdMatchingField, $"{{ {Constants.IdFieldPlaceholder}: $id }} ");

            var commandText =
                matchString +
                $"{Constants.ActionCommandPlaceholder} {string.Join(',', returnAliases.Select(o => o))}";

            var parameters = new Dictionary<string, string>
            {
                { "id", key.ToString() }
            };

            return new Neo4JTranslationResult
            {
                QueryCommandText = commandText,
                Parameters = parameters,
                AliasDictionary = aliasDictionary
            };
        }

        public Neo4JTranslationResult Translate<TP>(IQueryCommand<TD, TP> command)
        {
            var specification = command.Specification;
            var pagingOptions = command.PagingOptions;
            var graphStrategy =
                command.NavigationStrategy as GraphStrategy<TD> ??
                new GraphStrategy<TD>(new RelationshipNavigation<TD>());

            try
            {
                var aliasDictionary = BuildAliasDictionary(graphStrategy);

                // Map primary
                var primaryNavigation =
                    graphStrategy
                        .RelationshipNavigation;

                var primaryAlias =
                    aliasDictionary[primaryNavigation.GetHashCode()];

                var query = Translate(specification, primaryAlias.ToString());

                if (!string.IsNullOrEmpty(query))
                    query = $"WHERE {query}";

                var (matchString, returnAliases) = BuildMatchString(graphStrategy, aliasDictionary);

                var queryCommandText =
                    matchString +
                    $" {query} " +
                    $"RETURN {string.Join(',', returnAliases.Select(o => o))} " +
                    $"{Constants.PagingPlaceholder} " +
                    $"{Constants.OrderByPlaceholder} ";

                var countCommandText =
                    matchString +
                    $" {query} " +
                    $"RETURN COUNT({primaryAlias}) ";

                var countCommandIndex =
                    $"COUNT({primaryAlias})";

                var page = pagingOptions?.Page;
                var pageSize = pagingOptions?.PageSize;
                var sortDescending = pagingOptions?.Descending;

                var pagingString = string.Empty;
                var orderByString = string.Empty;
                if (pageSize.HasValue && page.HasValue) pagingString = $"SKIP {page.Value} LIMIT {pageSize.Value} ";

                if (pagingOptions?.OrderBy != null)
                {
                    var sort =
                        ClassEx
                            .ExtractPropertyInfo<TD>(pagingOptions?.OrderBy);

                    orderByString = $"ORDER BY {primaryAlias}.{sort.Name} " +
                                    $"{(sortDescending.GetValueOrDefault() ? "DESC" : string.Empty)}";
                }

                queryCommandText =
                    queryCommandText
                        .Replace(Constants.PagingPlaceholder, pagingString)
                        .Replace(Constants.OrderByPlaceholder, orderByString);

                return new Neo4JTranslationResult
                {
                    QueryCommandText = queryCommandText,
                    CountCommandText = countCommandText,
                    CountCommandIndex = countCommandIndex,
                    Parameters = new Dictionary<string, string>(),
                    AliasDictionary = aliasDictionary
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
            var graphStrategy =
                command.NavigationStrategy as GraphStrategy<TD> ??
                new GraphStrategy<TD>(new RelationshipNavigation<TD>());

            var aliasDictionary = BuildAliasDictionary(graphStrategy);

            var entityName =
                typeof(TD)
                    .Name;

            var commandText =
                $"MERGE ({Constants.NodeAliasPlaceholder}.:{entityName} " +
                $"{{ {Constants.SetterStringPlaceholder} }}) RETURN {Constants.NodeAliasPlaceholder}.";

            var parameters = new Dictionary<string, string>();

            return new Neo4JTranslationResult
            {
                QueryCommandText = commandText,
                Parameters = parameters,
                AliasDictionary = aliasDictionary
            };
        }

        private static Dictionary<int, char> BuildAliasDictionary(
            GraphStrategy<TD> graphStrategy)
        {
            var aliasCounter = 'a';

            var completeList = new List<int>();

            var currentNavigation = graphStrategy.RelationshipNavigation;

            foreach (var navNext in currentNavigation.GetNavigationChain())
                completeList
                    .Add(navNext.GetHashCode());

            // TODO: catch after 'z'

            return
                completeList
                    .ToDictionary(o => o, _ => aliasCounter++);
        }

        private Tuple<string, IEnumerable<string>> BuildMatchString(GraphStrategy<TD> graphStrategy, Dictionary<int, char> aliasDictionary)
        {
            var matchString = new StringBuilder();
            var returnAliases = new List<string>();

            matchString
                .Append("MATCH ");

            using var enumerator =
                graphStrategy
                    .RelationshipNavigation
                    .GetNavigationChain()
                    .GetEnumerator();

            if (enumerator.MoveNext())
            {
                var currentNavigation = enumerator.Current;
                var currentAlias = aliasDictionary[currentNavigation.GetHashCode()];

                returnAliases
                    .Add(currentAlias.ToString());

                matchString
                    .Append($"({currentAlias}:{currentNavigation.DataType.Name} ")
                    .Append(Constants.IdMatchingField)
                    .Append(") ");

                while (enumerator.MoveNext())
                {
                    var navigation = enumerator.Current;
                    if (navigation == null) continue;

                    var navigationAlias = aliasDictionary[navigation.GetHashCode()];
                    var navigationEntityTypeName = navigation.DataType.Name;

                    var relationshipType = currentNavigation.Relationship.Type;
                    var relationshipAlias = $"{currentAlias}_{navigationAlias}";

                    matchString
                        .Append($"-[{relationshipAlias}:{relationshipType}]->({navigationAlias}:{navigationEntityTypeName})");

                    returnAliases
                        .Add(relationshipAlias);
                    returnAliases
                        .Add(navigationAlias.ToString());

                    currentNavigation = navigation;
                    currentAlias = navigationAlias;
                }
            }

            return new Tuple<string, IEnumerable<string>>(
                matchString
                    .ToString(),
                returnAliases
            );
        }

        private string Translate(Specification<TD> spec, string alias)
        {
            var name = string.Empty;
            return spec switch
            {
                SpecificationByAll<TD> => "1=1",
                SpecificationByNone<TD> => "0=1",
                SpecificationByEquals<TD, string> eq =>
                    $"{alias}.{eq.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} = '{eq.Value}'",
                SpecificationByEquals<TD, int> eq =>
                    $"{alias}.{eq.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} = '{eq.Value}'",
                SpecificationByEquals<TD, Guid> eq =>
                    $"{alias}.{eq.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} = '{eq.Value.ToString()}'",
                SpecificationByAnd<TD> and => $"({Translate(and.Left, alias)} AND {Translate(and.Right, alias)})",
                SpecificationByOr<TD> and => $"({Translate(and.Left, alias)} OR {Translate(and.Right, alias)})",
                SpecificationByNot<TD> not => $"NOT ({Translate(not.Spec, alias)})",
                SpecificationByTextStartsWith<TD> text =>
                    $"{alias}.{text.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} STARTS WITH '{text.Value}'",
                SpecificationByTextEndsWith<TD> text =>
                    $"{alias}.{text.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} ENDS WITH '{text.Value}'",
                SpecificationByTextContains<TD> text =>
                    $"{alias}.{text.Property.Body.GetPropertyExpressionPath(ref name, lookForAlias: false)} CONTAINS '{text.Value}'",
                SpecificationThatIsNotCompatible<TD> => throw new UnparsableSpecificationException(),
                _ => throw new NotImplementedException()
            };
        }
    }
}