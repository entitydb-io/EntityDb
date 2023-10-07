using EntityDb.Abstractions.ValueObjects;
using EntityDb.Npgsql.Queries;
using EntityDb.SqlDb.Converters;
using EntityDb.SqlDb.Documents;
using EntityDb.SqlDb.Documents.Lease;
using EntityDb.SqlDb.Documents.Tag;
using EntityDb.SqlDb.Queries.Definitions.Filter;
using EntityDb.SqlDb.Queries.Definitions.Sort;
using Npgsql;
using NpgsqlTypes;
using System.Collections;
using System.Data.Common;
using System.Text;

namespace EntityDb.Npgsql.Converters;

internal class NpgsqlConverter : ISqlConverter<NpgsqlQueryOptions>
{
    public string SqlType => "Npgsql";

    private static string GetProjection(IDocumentReader documentReader)
    {
        return string.Join
        (
            ", ",
            documentReader.GetPropertyNames()
        );
    }

    private static string GetEqFilter(EqFilterDefinition eqFilterDefinition, NpgsqlParameterCollection parameters)
    {
        var parameterName = AddParameter(parameters, eqFilterDefinition.PropertyName, eqFilterDefinition.PropertyValue);

        return $"{eqFilterDefinition.PropertyName} = {parameterName}";
    }

    private static string GetInFilter(InFilterDefinition inFilterDefinition, NpgsqlParameterCollection parameters)
    {
        var parameterNames = AddParameters(parameters, inFilterDefinition.PropertyName, inFilterDefinition.PropertyValues);

        return $"{inFilterDefinition.PropertyName} IN ({string.Join(", ", parameterNames)})";
    }

    private static string GetAnyInFilter(AnyInFilterDefinition anyInFilterDefinition, NpgsqlParameterCollection parameters)
    {
        var parameterName = AddParameter(parameters, anyInFilterDefinition.PropertyName, anyInFilterDefinition.PropertyValues);

        return $"{anyInFilterDefinition.PropertyName} && {parameterName}";
    }

    private static string GetGteFilter(GteFilterDefinition gteFilterDefinition, NpgsqlParameterCollection parameters)
    {
        var parameterName = AddParameter(parameters, gteFilterDefinition.PropertyName, gteFilterDefinition.PropertyValue);

        return $"{gteFilterDefinition.PropertyName} >= {parameterName}";
    }

    private static string GetLteFilter(LteFilterDefinition lteFilterDefinition, NpgsqlParameterCollection parameters)
    {
        var parameterName = AddParameter(parameters, lteFilterDefinition.PropertyName, lteFilterDefinition.PropertyValue);

        return $"{lteFilterDefinition.PropertyName} <= {parameterName}";
    }

    private static string GetFilter(IFilterDefinition filterDefinition, NpgsqlParameterCollection parameters)
    {
        var filter = filterDefinition switch
        {
            AndFilterDefinition andFilterDefinition => string.Join
            (
                " AND ",
                andFilterDefinition.FilterDefinitions.Select(innerFilterDefinition => GetFilter(innerFilterDefinition, parameters))
            ),

            OrFilterDefinition orFilterDefinition => string.Join
            (
                " OR ",
                orFilterDefinition.FilterDefinitions.Select(innerFilterDefinition => GetFilter(innerFilterDefinition, parameters))
            ),

            NotFilterDefinition notFilterDefinition => $"NOT {GetFilter(notFilterDefinition.FilterDefinition, parameters)}",

            EqFilterDefinition eqFilterDefinition => GetEqFilter(eqFilterDefinition, parameters),

            InFilterDefinition inFilterDefinition => GetInFilter(inFilterDefinition, parameters),

            AnyInFilterDefinition anyInFilterDefinition => GetAnyInFilter(anyInFilterDefinition, parameters),

            GteFilterDefinition gteFilterDefinition => GetGteFilter(gteFilterDefinition, parameters),

            LteFilterDefinition lteFilterDefinition => GetLteFilter(lteFilterDefinition, parameters),

            _ => throw new NotSupportedException()
        };

        return $"({filter})";
    }

    private static string GetCollate(string tableName, NpgsqlQueryOptions? options, string propertyName)
    {
        if (tableName == LeaseDocument.TableName && propertyName == nameof(LeaseDocument.Value) && options?.LeaseValueSortCollation != null)
        {
            return $"COLLATE {options.LeaseValueSortCollation}";
        }

        if (tableName == TagDocument.TableName && propertyName == nameof(TagDocument.Value) && options?.TagValueSortCollation != null)
        {
            return $"COLLATE {options.TagValueSortCollation}";
        }

        return string.Empty;
    }

    private static string GetAscSort(string tableName, NpgsqlQueryOptions? options, AscSortDefinition ascSortDefinition)
    {

        return $"{ascSortDefinition.PropertyName} {GetCollate(tableName, options, ascSortDefinition.PropertyName)} ASC";
    }

    private static string GetDescSort(string tableName, NpgsqlQueryOptions? options, DescSortDefinition descSortDefinition)
    {
        return $"{descSortDefinition.PropertyName} {GetCollate(tableName, options, descSortDefinition.PropertyName)} DESC";
    }

    private static string GetSort(string tableName, NpgsqlQueryOptions? options, ISortDefinition sortDefinition)
    {
        return sortDefinition switch
        {
            CombineSortDefinition combineSortDefinition => string.Join
            (
                ", ",
                combineSortDefinition.SortDefinitions.Select(innerSortDefinition => GetSort(tableName, options, innerSortDefinition))
            ),

            AscSortDefinition ascSortDefinition => GetAscSort(tableName, options, ascSortDefinition),

            DescSortDefinition descSortDefinition => GetDescSort(tableName, options, descSortDefinition),

            _ => throw new NotSupportedException()
        };
    }

    private static string AddParameter(NpgsqlParameterCollection parameters, string propertyName, object propertyValue)
    {
#if DEBUG
        var parameterName = $"@{propertyName}_{parameters.Count}";
#else
        var parameterName = $"@{parameters.Count}";
#endif

        var parameter = propertyValue switch
        {
            IEnumerable<Id> ids => new NpgsqlParameter(parameterName, NpgsqlDbType.Array | NpgsqlDbType.Uuid)
            {
                Value = ids
                    .Select(id => id.Value)
                    .ToArray()
            },
            Id id => new NpgsqlParameter(parameterName, NpgsqlDbType.Uuid)
            {
                Value = id.Value
            },
            TimeStamp timeStamp => new NpgsqlParameter(parameterName, NpgsqlDbType.TimestampTz)
            {
                Value = timeStamp.Value
            },
            VersionNumber versionNumber => new NpgsqlParameter(parameterName, NpgsqlDbType.Bigint)
            {
                Value = Convert.ToInt64(versionNumber.Value)
            },
            string value => propertyName == nameof(ITransactionDocument.Data)
                ? new NpgsqlParameter(parameterName, NpgsqlDbType.Jsonb)
                {
                    Value = value
                }
                : new NpgsqlParameter(parameterName, NpgsqlDbType.Varchar)
                {
                    Value = value
                },
            _ => throw new NotSupportedException()
        };

        parameters.Add(parameter);

        return parameter.ParameterName;
    }

    private static IEnumerable<string> AddParameters(NpgsqlParameterCollection parameters, string propertyName, IEnumerable propertyValues)
    {
        foreach (var propertyValue in propertyValues)
        {
            yield return AddParameter(parameters, propertyName, propertyValue);
        }
    }

    public DbCommand ConvertInsert<TDocument>
    (
        string tableName,
        TDocument[] documents
    )
        where TDocument : ITransactionDocument
    {
        var dbCommand = new NpgsqlCommand();

        var columnNames = new List<string>();
        var parameterNameSets = new List<string>(documents.Length);

        foreach (var index in Enumerable.Range(0, documents.Length))
        {
            var document = documents[index];

            var documentDictionary = document.ToDictionary();

            var parameterNames = new List<string>(documentDictionary.Count);

            foreach (var (propertyName, propertyValue) in documentDictionary)
            {
                var parameterName = AddParameter(dbCommand.Parameters, propertyName, propertyValue);

                parameterNames.Add(parameterName);

                if (index == 0)
                {
                    columnNames.Add(propertyName);
                }
            }

            parameterNameSets.Add($"({string.Join(", ", parameterNames)})");
        }

        dbCommand.CommandText = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES {string.Join(", ", parameterNameSets)}";

        return dbCommand;
    }

    public DbCommand ConvertQuery
    (
        string tableName,
        IDocumentReader documentReader,
        IFilterDefinition filterDefinition,
        ISortDefinition? sortDefinition,
        int? skip,
        int? limit,
        NpgsqlQueryOptions? options
    )
    {
        var dbQuery = new NpgsqlCommand();

        var sqlBuilder = new StringBuilder($"SELECT {GetProjection(documentReader)} FROM {tableName} WHERE {GetFilter(filterDefinition, dbQuery.Parameters)}");

        if (sortDefinition != null)
        {
            sqlBuilder.Append($" ORDER BY {GetSort(tableName, options, sortDefinition)}");
        }

        if (skip != null)
        {
            sqlBuilder.Append($" OFFSET {skip.Value}");
        }

        if (limit != null)
        {
            sqlBuilder.Append($" LIMIT {limit.Value}");
        }

        dbQuery.CommandText = sqlBuilder.ToString();

        return dbQuery;
    }

    public DbCommand ConvertDelete
    (
        string tableName,
        IFilterDefinition filterDefinition
    )
    {
        var dbCommand = new NpgsqlCommand();

        dbCommand.CommandText = $"DELETE FROM {tableName} WHERE {GetFilter(filterDefinition, dbCommand.Parameters)}";

        return dbCommand;
    }
}
