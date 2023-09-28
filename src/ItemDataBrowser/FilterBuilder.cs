using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace ItemDataBrowser
{
    public class FilterBuilder
    {
        private const string OrLiteral = " | ";
        private const string AndLiteral = " & ";

        private readonly Regex _partRegex = new Regex(@"^(\w+)(?:(==|!=|\?=|SW|EW|>>|<<|>=|<=)([\w\d]+)|(BT)(.+),(.+))(?:\s(\&|\|)\s?)?$");

        private readonly List<ParsingError> _errors = new();

        private static ConstantExpression _stringComparisonExpression = Expression.Constant(StringComparison.CurrentCultureIgnoreCase, typeof(StringComparison));

        public bool Validate<TEntity>(string filter)
        {
            _errors.Clear();

            _ = ParseInput(filter);

            return !_errors.Any();
        }

        public Expression<Func<TEntity, bool>>? Get<TEntity>(string filter)
            where TEntity : class
        {
            _errors.Clear();

            var filterDataCollection = ParseInput(filter);

            if (_errors.Any())
                return null;

            return BuildExpression<TEntity>(filterDataCollection);
        }

        public LambdaExpression? Get(Type type, string filter)
        {
            _errors.Clear();

            var filterDataCollection = ParseInput(filter);

            if (_errors.Any())
                return null;

            return BuildExpression(type, filterDataCollection);
        }

        public List<ParsingError> GetErrors() => _errors;

        private Expression<Func<TEntity, bool>> BuildExpression<TEntity>(List<FilterData> filterDataCollection)
            where TEntity : class
        {
            var entityType = typeof(TEntity);
            var parameterExpression = Expression.Parameter(entityType, "x");
            var expression = GetExpression(entityType, parameterExpression, filterDataCollection);

            return Expression.Lambda<Func<TEntity, bool>>(expression, parameterExpression);
        }

        private LambdaExpression BuildExpression(Type entityType, List<FilterData> filterDataCollection)
        {
            var parameterExpression = Expression.Parameter(entityType, "x");
            var expression = GetExpression(entityType, parameterExpression, filterDataCollection);

            return Expression.Lambda(expression, parameterExpression);
        }

        private Expression GetExpression(Type entityType, Expression parameterExpression, List<FilterData> filterDataCollection)
        {
            Expression expression = null;

            var nextCombination = NextFilterCombination.None;

            if (parameterExpression.Type == typeof(object))
                parameterExpression = Expression.Convert(parameterExpression, entityType);

            foreach (var filterData in filterDataCollection)
            {
                var property = GetProperty(filterData, entityType);
                var propertyExpression = Expression.Property(parameterExpression, property);
                var value0 = PrepareValue(filterData.Values[0], property.PropertyType);
                var constant0Expression = Expression.Constant(value0, property.PropertyType);
                Expression tempExpression = null;

                if (filterData.Function == FilterFunction.Between)
                {
                    var value1 = PrepareValue(filterData.Values[1], property.PropertyType);
                    var constant1Expression = Expression.Constant(value1, property.PropertyType);
                    var lowerExpression = Expression.GreaterThan(propertyExpression, constant0Expression);
                    var higherExpression = Expression.LessThan(propertyExpression, constant1Expression);

                    tempExpression = Expression.AndAlso(lowerExpression, higherExpression);
                }
                else
                {
                    switch (filterData.Function)
                    {
                        case FilterFunction.Equal:
                            tempExpression = Expression.Equal(propertyExpression, constant0Expression);
                            break;
                        case FilterFunction.NotEqual:
                            tempExpression = Expression.NotEqual(propertyExpression, constant0Expression);
                            break;
                        case FilterFunction.Like:
                            var likeConstantExpression = Expression.Constant($"{value0}", property.PropertyType);
                            var containsMethod = typeof(String)
                                .GetMethods()
                                .First(m => m.Name == nameof(String.Contains) &&
                                            m.GetParameters().Length == 2 &&
                                            m.GetParameters()[0].ParameterType == typeof(String) &&
                                            m.GetParameters()[1].ParameterType == typeof(StringComparison));

                            tempExpression = Expression.Call(propertyExpression, containsMethod, likeConstantExpression, _stringComparisonExpression);
                            break;
                        case FilterFunction.StartsWith:
                            var startsWithConstantExpression = Expression.Constant($"{value0}", property.PropertyType);
                            var startsWithMethod = typeof(String)
                                .GetMethods()
                                .First(m => m.Name == nameof(String.StartsWith) &&
                                            m.GetParameters().Length == 1 &&
                                            m.GetParameters()[0].ParameterType == typeof(String) &&
                                            m.GetParameters()[1].ParameterType == typeof(StringComparison));

                            tempExpression = Expression.Call(propertyExpression, startsWithMethod, startsWithConstantExpression, _stringComparisonExpression);
                            break;
                        case FilterFunction.EndsWith:
                            var endsWithConstantExpression = Expression.Constant($"{value0}", property.PropertyType);
                            var endsWithMethod = typeof(String)
                                .GetMethods()
                                .First(m => m.Name == nameof(String.EndsWith) &&
                                            m.GetParameters().Length == 1 &&
                                            m.GetParameters()[0].ParameterType == typeof(String) &&
                                            m.GetParameters()[1].ParameterType == typeof(StringComparison));

                            tempExpression = Expression.Call(propertyExpression, endsWithMethod, endsWithConstantExpression, _stringComparisonExpression);
                            break;
                        case FilterFunction.Greater:
                            tempExpression = Expression.GreaterThan(propertyExpression, constant0Expression);
                            break;
                        case FilterFunction.Smaller:
                            tempExpression = Expression.LessThan(propertyExpression, constant0Expression);
                            break;
                        case FilterFunction.GreaterEqual:
                            tempExpression = Expression.GreaterThanOrEqual(propertyExpression, constant0Expression);
                            break;
                        case FilterFunction.SmallerEqual:
                            tempExpression = Expression.LessThanOrEqual(propertyExpression, constant0Expression);
                            break;
                    }
                }

                if (tempExpression == null)
                    continue;

                if (expression == null)
                {
                    expression = tempExpression;
                }
                else
                {
                    switch (nextCombination)
                    {
                        case NextFilterCombination.And:
                            expression = Expression.AndAlso(expression, tempExpression);
                            break;
                        case NextFilterCombination.Or:
                            expression = Expression.OrElse(expression, tempExpression);
                            break;
                    }
                }

                nextCombination = filterData.Combination;
            }

            if (expression == null)
                throw new ParseException(nameof(BuildExpression), "Could not create expression.");

            return expression;
        }

        private object PrepareValue(string value, Type targetType)
        {
            if (targetType == typeof(String))
                return value.Replace("\'", "");

            if (targetType == typeof(Boolean))
                return value.ToLower() == "true";

            if (targetType == typeof(Int32))
                return Int32.Parse(value);

            throw new ParseException(nameof(PrepareValue), $"Type '{targetType.Name}' can not be handled.");
        }

        private PropertyInfo GetProperty(FilterData filter, Type entityType)
        {
            var propertyInfo = entityType.GetProperty(filter.PropertyName);

            if (propertyInfo == null)
                throw new ParseException($"{nameof(GetProperty)}_{filter.PropertyName}", $"Could not find property '{filter.PropertyName}' on entity '{entityType.Name}'.");

            return propertyInfo;
        }

        private List<FilterData> ParseInput(string filter)
        {
            var startPosition = 0;
            var endPosition = filter.Length;
            var filterDataCollection = new List<FilterData>();

            while (startPosition <= endPosition)
            {
                try
                {
                    var andIndex = filter.IndexOf(AndLiteral, 0, StringComparison.Ordinal);
                    var orIndex = filter.IndexOf(OrLiteral, 0, StringComparison.Ordinal);

                    // no more matches found
                    if (andIndex == -1 && orIndex == -1)
                    {
                        filterDataCollection.Add(Parse(filter));
                        break;
                    }

                    // handle AND
                    if (andIndex > -1 && (andIndex < orIndex || orIndex == -1))
                    {
                        var filterPart = filter.Substring(0, andIndex + AndLiteral.Length);

                        filter = filter.Remove(0, filterPart.Length);
                        filterDataCollection.Add(Parse(filterPart));
                        startPosition = andIndex + 1;
                        continue;
                    }

                    // handle OR
                    if (orIndex > -1 && (orIndex < andIndex || andIndex == -1))
                    {
                        var filterPart = filter.Substring(0, orIndex + OrLiteral.Length);

                        filter = filter.Remove(0, filterPart.Length);
                        filterDataCollection.Add(Parse(filterPart));
                        startPosition = orIndex + 1;
                        continue;
                    }
                }
                catch (ParseException pex)
                {
                    _errors.Add(new ParsingError
                    {
                        Name = pex.Name,
                        Error = pex.Message
                    });
                    break;
                }
            }

            return filterDataCollection;
        }

        private FilterData Parse(string part)
        {
            var match = _partRegex.Match(part);

            if (match.Success)
            {
                var filterData = new FilterData
                {
                    PropertyName = match.Groups[1].Value,
                    Combination = MapCombination(match.Groups[7])
                };

                // single value matches
                if (match.Groups[2].Success)
                {
                    filterData.Function = MapFunction(match.Groups[2]);
                    filterData.Values = new[]
                    {
                        match.Groups[3].Value
                    };

                    return filterData;
                }

                if (match.Groups[4].Success)
                {
                    filterData.Function = MapFunction(match.Groups[4]);
                    filterData.Values = new[]
                    {
                        match.Groups[5].Value,
                        match.Groups[6].Value
                    };

                    return filterData;
                }
            }

            throw new ParseException(nameof(Parse), $"Failed to parse: \"{part}\"");
        }

        private FilterFunction MapFunction(Group matchGroup)
        {
            if (!matchGroup.Success)
                throw new ParseException(nameof(MapFunction), "Can not map function.");

            switch (matchGroup.Value)
            {
                case "==":
                    return FilterFunction.Equal;
                case "!=":
                    return FilterFunction.NotEqual;
                case "?=":
                    return FilterFunction.Like;
                case "SW":
                    return FilterFunction.StartsWith;
                case "EW":
                    return FilterFunction.EndsWith;
                case "BT":
                    return FilterFunction.Between;
                case ">>":
                    return FilterFunction.Greater;
                case "<<":
                    return FilterFunction.Smaller;
                case ">=":
                    return FilterFunction.GreaterEqual;
                case "<=":
                    return FilterFunction.SmallerEqual;
                default:
                    throw new ParseException(nameof(MapFunction), "Can not map function.");
            }
        }

        private NextFilterCombination MapCombination(Group matchGroup)
        {
            switch (matchGroup.Value)
            {
                case "&":
                    return NextFilterCombination.And;
                case "|":
                    return NextFilterCombination.Or;
                default:
                    return NextFilterCombination.None;
            }
        }
    }

    public class FilterData
    {
        public string PropertyName { get; set; }

        public string[] Values { get; set; }

        public FilterFunction Function { get; set; }

        public NextFilterCombination Combination { get; set; }
    }

    public enum FilterFunction
    {
        Equal,
        NotEqual,
        Like,
        StartsWith,
        EndsWith,
        Between,
        Greater,
        Smaller,
        GreaterEqual,
        SmallerEqual
    }

    public enum NextFilterCombination
    {
        None,
        And,
        Or
    }

    public class ParsingError
    {
        public string Name { get; set; }

        public string Error { get; set; }
    }

    [Serializable]
    public class ParseException : Exception
    {
        public string Name { get; private set; }

        public ParseException(string name, string message) : base(message)
        {
            Name = name;
        }

        public ParseException(string name, string message, Exception inner) : base(message, inner)
        {
            Name = name;
        }

        protected ParseException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
