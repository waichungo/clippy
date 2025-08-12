using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Clippy
{
    public class DBResult<T> where T : class, new()
    {
        [JsonProperty("totalCount")]
        public long TotalCount { get; set; } = 0;
        [JsonProperty("total")]
        public int Total { get; set; } = 0;
        [JsonProperty("page")]
        public int Page { get; set; } = 1;
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; } = 1;
        [JsonProperty("entries")]
        public List<T> Entries { get; set; } = new();
    }

    public enum DBEQUALITY
    {
        EQUAL, LESSTHAN, GREATERTHAN, LESSTHAN_OR_EQUAL, GREATERTHAN_OR_EQUAL, IN, NOT_IN
    }
    public enum DBQUERYTYPE
    {
        AND, OR
    }
    public abstract class Query
    {
        public abstract Query? Q { get; set; }
        public abstract DBQUERYTYPE Type { get; set; }
        public abstract List<QueryParam> Queries { get; set; }
    }
    public class AndQuery : Query
    {
        public override Query? Q { get; set; } = null;
        public override DBQUERYTYPE Type { get; set; } = DBQUERYTYPE.AND;
        public override List<QueryParam> Queries { get; set; } = [];
    }
    public class OrQuery : Query
    {
        public override Query? Q { get; set; } = null;
        public override DBQUERYTYPE Type { get; set; } = DBQUERYTYPE.OR;
        public override List<QueryParam> Queries { get; set; } = [];
    }

    public class QueryParam
    {
        public string Key { get; set; } = "";
        public string? BindName { get; set; } = null;
        public DBEQUALITY EQUALITY { get; set; } = DBEQUALITY.EQUAL;
        public object Value { get; set; } = null;
        public List<object> Values { get; set; } = new();

    }

    public class DBIndexDescriptor
    {
        public DBIndexDescriptor(string field)
        {
            Field = field;
        }
        public string Field { get; set; } = "";
        public string IndexName { get; set; } = "";
    }
    public class ForeignField
    {
        //ALTER TABLE child ADD COLUMN parent_id INTEGER REFERENCES parent(id);
        public string KeyType { get; set; } = "int";
        public string Table { get; set; } = "";
        public string Field { get; set; } = "";
        public string RefTable { get; set; } = "";
        public string RefField { get; set; } = "";
        public bool IsNullable { get; set; } = true;

    }
    public class DBField
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsNullable { get; set; } = false;
        public bool IsPrimaryKey { get; set; } = false;
    }
    public class DBCreateConfig
    {
        public string TableName { get; set; } = "";
        public List<DBIndexDescriptor> Indexes { get; set; } = new();
        public List<DBIndexDescriptor> UniqueIndexes { get; set; } = new();
        public List<DBField> Fields { get; set; } = new();
        public List<ForeignField> ForeignFields { get; set; } = new();

    }

    public class DBHelpers
    {
        private static Dictionary<string, string> TypesMap = new() {
            { "int", "INTEGER" },
            { "long", "INTEGER" },
            { "byte", "INTEGER" },
            { "short", "INTEGER" },
            { "bool", "INTEGER" },
            { "double", "REAL" },
            { "byte[]", "BLOB" },
            { nameof(TimeSpan), "INTEGER" },
            { nameof(DateTime), "INTEGER" },
            { "string", "TEXT" },
        };
        public static string GetDBTypeFromObjectType(string objectType)
        {
            if (objectType.EndsWith("?"))
            {
                objectType = objectType.Replace("?", "");
            }
            return TypesMap[objectType];
        }
        //static Dictionary<Type, string> PropMap = new() {
        //    { typeof(int), "INTEGER" },
        //    { typeof(long), "INTEGER" },
        //    { typeof(byte), "INTEGER" },
        //    { typeof(short), "INTEGER" },
        //    { typeof(bool), "INTEGER" },
        //    { typeof(double), "REAL" },
        //    { typeof(TimeSpan), "INTEGER" },
        //    { typeof(DateTime), "INTEGER" },
        //    { typeof(string), "TEXT" },
        //};
        private static string GenerateForeignKeysSql(DBCreateConfig cfg)
        {
            var res = "";

            var sql = "";
            foreach (var item in cfg.ForeignFields)
            {
                var deleteOp = "CASCADE";
                if (item.IsNullable)
                {
                    deleteOp = "SET NULL";
                }
                sql = $"ALTER TABLE {item.Table} ADD COLUMN {item.Field} {GetDBTypeFromObjectType(item.KeyType)} REFERENCES {item.RefTable}({item.RefField})  ON DELETE {deleteOp}  ON UPDATE {deleteOp};";
                res += sql + "\n";
            }
            return res;
        }
        private static string GenerateIndexSql(DBCreateConfig cfg)
        {
            var res = "";
            var indexes = cfg.Indexes;
            var uIndexes = cfg.UniqueIndexes;
            if (indexes.Count() == 0 && uIndexes.Count() == 0)
            {
                return res;
            }
            Dictionary<string, List<string>> indexMap = new();
            var propKeys = cfg.Fields.Select(e => e.Name).ToList();
            foreach (var item in indexes)
            {
                if (!propKeys.Contains(item.Field))
                    throw new InvalidOperationException($"'{item.Field}' is not a valid index for '{cfg.TableName}' table");
            }
            foreach (var item in uIndexes)
            {
                if (!propKeys.Contains(item.Field))
                    throw new InvalidOperationException($"'{item.Field}' is not a valid unique index for '{cfg.TableName}' table");
            }

            foreach (var field in indexes)
            {
                if (field.IndexName.Length > 0)
                {
                    if (!indexMap.ContainsKey(field.IndexName))
                    {
                        indexMap[field.IndexName] = new();
                    }
                    indexMap[field.IndexName].Add(field.Field);
                    continue;
                }
                res += $"""
                    CREATE INDEX IF NOT EXISTS "{cfg.TableName}_{field.Field}_Idx" ON "{cfg.TableName}" (
                    	"{field}"	ASC
                    );
                    """;
                res += "\n";
            }
            foreach (var entry in indexMap)
            {
                var fieldsStr = string.Join(", ", entry.Value.Select(e => $"\"{e}\" ASC"));
                res += $"""
                    CREATE INDEX IF NOT EXISTS "{cfg.TableName}_{entry.Key}_Idx" ON "{cfg.TableName}" (
                    	{fieldsStr}
                    );
                    """;
                res += "\n";
            }
            indexMap.Clear();
            foreach (var field in uIndexes)
            {
                if (field.IndexName.Length > 0)
                {
                    if (!indexMap.ContainsKey(field.IndexName))
                    {
                        indexMap[field.IndexName] = new();
                    }
                    indexMap[field.IndexName].Add(field.Field);
                    continue;
                }
                res += $"""
                    CREATE UNIQUE INDEX IF NOT EXISTS "{cfg.TableName}_{field.Field}_Idx" ON "{cfg.TableName}" (
                    	"{field}"	ASC
                    );
                    """;
                res += "\n";
            }
            foreach (var entry in indexMap)
            {
                var fieldsStr = string.Join(", ", entry.Value.Select(e => $"\"{e}\" ASC"));
                res += $"""
                    CREATE UNIQUE INDEX IF NOT EXISTS "{cfg.TableName}_{entry.Key}_Idx" ON "{cfg.TableName}" (
                    	{fieldsStr}
                    );
                    """;
                res += "\n";
            }

            return res;

        }
        private static string GenerateFieldsSql(DBCreateConfig cfg)
        {
            var res = "";
            List<DBField> pKeys = [];
            for (int i = 0; i < cfg.Fields.Count; i++)
            {
                var isNonNull = true;
                DBField field = cfg.Fields[i];
                if (field.IsPrimaryKey)
                {
                    pKeys.Add(field);
                }
                var type = GetDBTypeFromObjectType(field.Type);
                isNonNull = !field.IsNullable;

                res += $"\t\"{field.Name}\" {type}";
                //"Id"	INTEGER NOT NULL,
                if (isNonNull)
                {
                    res += " NOT NULL ";
                }
                if (i == cfg.Fields.Count - 1 && pKeys.Count == 0)
                {
                    res += "\n";
                }
                else
                {
                    res += ",\n";
                }

            }
            if (pKeys.Count > 0)
            {
                var keyStr = string.Join(", ", pKeys.Select(e => $"\"{e.Name}\" {((e.Type == "int" || e.Type == "long") ? "AUTOINCREMENT" : "")}"));
                res += $"\tCONSTRAINT \"PK_{cfg.TableName}\" PRIMARY KEY( {keyStr} )\n";
            }
            return res;

        }
        public static string GetTableSql(DBCreateConfig cfg)
        {
            var sql = "";
            var tableSql = $"""
                CREATE TABLE IF NOT EXISTS "{cfg.TableName}" (
                {GenerateFieldsSql(cfg)}
                );
                """;
            var modelIndexSql = GenerateIndexSql(cfg);
            var fKeysSql = "";
            if (cfg.ForeignFields.Count > 0)
            {
                fKeysSql = GenerateForeignKeysSql(cfg).Trim();
            }
            List<string> sqls = [tableSql, modelIndexSql, fKeysSql];
            sql += string.Join("\n", sqls.Select(e => e.Trim()).Where(e => e.Length > 0));
            return sql;
        }
        public static void CreateTableFromModel(SQLiteConnection Conn, DBCreateConfig cfg)
        {
            var tableSql = $"""
                CREATE TABLE IF NOT EXISTS "{cfg.TableName}" (
                {GenerateFieldsSql(cfg)}
                );
                """;
            var modelIndexSql = GenerateIndexSql(cfg);


            using (var cmd = new SQLiteCommand { Connection = Conn })
            {
                cmd.CommandText = tableSql;
                cmd.ExecuteNonQuery();
                if (modelIndexSql.Length > 0)
                {
                    cmd.CommandText = modelIndexSql;
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void CreateForeignKeys(SQLiteConnection Conn, DBCreateConfig cfg)
        {
            if (cfg.ForeignFields.Count > 0)
            {
                var sql = GenerateForeignKeysSql(cfg);

                using (var cmd = new SQLiteCommand { Connection = Conn })
                {
                    cmd.CommandText = sql;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
        public static void BindQueries(SQLiteCommand command, Query? queryParam)
        {
            if (queryParam == null) return;
            List<QueryParam> queries = [];
            if (queryParam != null)
            {
                Query? current = queryParam;
                while (current != null)
                {
                    queries.AddRange(current.Queries);
                    current = current.Q;
                }
            }
            object dataVal = null;
            foreach (var query in queries)
            {
                // query.Values.Select((el, idx) => $"@{query.Key}{idx+1}
                if ((query.EQUALITY == DBEQUALITY.IN || query.EQUALITY == DBEQUALITY.NOT_IN))
                {
                    for (int i = 0; i < query.Values.Count(); i++)
                    {
                        command.Parameters.AddWithValue($"@{query.BindName ?? query.Key}{i + 1}", query.Values[i]);
                    }
                }
                else
                {
                    dataVal = query.Value;
                    if (dataVal is DateTime time)
                    {
                        dataVal = ((DateTimeOffset)time).ToUnixTimeMilliseconds();
                    }
                    else if (dataVal is TimeSpan span)
                    {
                        dataVal = (long)span.TotalSeconds;
                    }
                    else if (dataVal != null && dataVal.GetType().IsEnum)
                    {
                        dataVal = (long)((int)dataVal);
                    }
                    else if (dataVal is bool boolVal)
                    {
                        dataVal = boolVal ? 1 : 0;
                    }
                    command.Parameters.AddWithValue("@" + (query.BindName ?? query.Key), dataVal);
                }
            }
        }
        public static string GetNextAvailableBindName(string name, HashSet<string> queryKeys, int? startIndex = null)
        {
            var key = name;
            var count = startIndex ?? 0;
            if (count > 0)
            {
                key = $"{name}{count}";
            }
            while (queryKeys.Contains(key))
            {
                count++;
                key = $"{name}{count}";
            }
            queryKeys.Add(key);
            return key;
        }
        public static Query NormalizeQuery(ref Query queryParam)
        {
            var queryKeys = new HashSet<string>();
            var key = "";


            if (queryParam != null)
            {
                Query? current = queryParam;
                while (current != null)
                {
                    for (int i = 0; i < current.Queries.Count; i++)
                    {
                        var query = current.Queries[i];
                        // query.Values.Select((el, idx) => $"@{query.Key}{idx+1}
                        if ((query.EQUALITY == DBEQUALITY.IN || query.EQUALITY == DBEQUALITY.NOT_IN))
                        {
                            key = "";
                            while (key.Length < 16)
                            {
                                var rnd = Guid.NewGuid().ToString().Where(e => char.IsLetter(e));
                                key += new string(rnd.ToArray());
                            }
                            key = key.Substring(0, 16);
                            query.BindName = key;
                        }
                        else
                        {
                            if (queryKeys.Contains(query.BindName ?? query.Key))
                            {
                                query.BindName = GetNextAvailableBindName(query.BindName ?? query.Key, queryKeys);
                            }
                            else
                            {
                                queryKeys.Add(query.BindName ?? query.Key);
                            }
                        }
                    }
                    current = current.Q;
                }
            }



            return queryParam;
        }
        public static string GetQueryLine(Query query)
        {
            var sqlSnippets = new List<string>();
            if (query.Queries.Count > 0)
            {
                foreach (var queryElement in query.Queries)
                {
                    var sql = "";
                    var equality = "";
                    if ((queryElement.Key == "") || (queryElement.Values.Count == 0 && (queryElement.EQUALITY == DBEQUALITY.IN || queryElement.EQUALITY == DBEQUALITY.NOT_IN))) continue;
                    switch (queryElement.EQUALITY)
                    {
                        case DBEQUALITY.EQUAL:
                            equality = "=";
                            break;
                        case DBEQUALITY.LESSTHAN:
                            equality = "<";
                            break;
                        case DBEQUALITY.GREATERTHAN:
                            equality = ">";
                            break;
                        case DBEQUALITY.LESSTHAN_OR_EQUAL:
                            equality = "<=";
                            break;
                        case DBEQUALITY.GREATERTHAN_OR_EQUAL:
                            equality = ">=";
                            break;
                        case DBEQUALITY.IN:
                            equality = "IN";
                            break;
                        case DBEQUALITY.NOT_IN:
                            equality = "NOT IN";
                            break;
                    }
                    if ((queryElement.EQUALITY == DBEQUALITY.IN || queryElement.EQUALITY == DBEQUALITY.NOT_IN))
                    {
                        var vals = queryElement.Values;
                        if (vals[0].GetType().IsEnum)
                        {
                            for (int i = 0; i < vals.Count; i++)
                            {
                                vals[i] = (long)vals[i];
                            }
                        }
                        else if (vals[0] is DateTime)
                        {
                            for (int i = 0; i < vals.Count; i++)
                            {
                                vals[i] = ((DateTimeOffset)((DateTime)vals[i])).ToUnixTimeSeconds();
                            }
                        }
                        else if (vals[0] is bool)
                        {
                            for (int i = 0; i < vals.Count; i++)
                            {
                                vals[i] = ((bool)vals[i]) ? 1 : 0;
                            }
                        }
                        else if (vals[0] is TimeSpan)
                        {
                            for (int i = 0; i < vals.Count; i++)
                            {
                                vals[i] = (long)((TimeSpan)vals[i]).TotalSeconds;
                            }
                        }

                        var concat = string.Join(" , ", queryElement.Values.Select((el, idx) => (queryElement.BindName == null ? $"@{queryElement.Key}{idx + 1}" : $"@{queryElement.BindName}{idx + 1}")));
                        var line = $" {queryElement.Key} {equality} ({concat}) ";
                        sql += line;
                    }
                    else
                    {
                        var val = queryElement.Value;
                        if (val.GetType().IsEnum)
                        {
                            val = (int)val;
                        }
                        else if (val is DateTime)
                        {
                            val = ((DateTimeOffset)val).ToUnixTimeSeconds();
                        }
                        else if (val is bool)
                        {
                            val = ((bool)val) ? 1 : 0;
                        }
                        else if (val is TimeSpan)
                        {
                            val = (long)((TimeSpan)val).TotalSeconds;
                        }

                        var line = $" {queryElement.Key} {equality} @{queryElement.BindName ?? queryElement.Key} ";
                        sql += line;

                    }
                    sqlSnippets.Add(sql);
                }
            }
            if (query.Q != null)
            {
                sqlSnippets.Add($"( {GetQueryLine(query.Q)} )");
            }
            return string.Join(((query is AndQuery) ? " AND " : " OR "), sqlSnippets);
        }
    }
}
