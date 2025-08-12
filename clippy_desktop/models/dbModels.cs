using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using Clippy;
using Clippy.db.functions;
namespace Clippy.db
{
    public interface IConnectionInterface
    {
        public IDbConnection? Connection
        {
            get;
        }
    }
    public class SqliteConnectionInterface : IConnectionInterface
    {
        public IDbConnection? Connection => DBAccess.Connection;
    }
    public class DBAccess
    {
        private static SQLiteConnection? Conn = null;
        private static SemaphoreSlim lck = new(1);
        public static SQLiteConnection? Connection => Conn;
        static string GetDbSource()
        {
            var DbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Clippy", "clippy.db");
            if (!Directory.Exists(Path.GetDirectoryName(DbPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            }
            var source = $"Data Source={DbPath}";
            return source;
        }
        private static void CreateModels()
        {
            ClipItemDBUtility.CreateClipItemsTable();
            List<DBCreateConfig> cfgs = new();
            cfgs.Add(ClipItemDBUtility.GetDBConfig());
            foreach (DBCreateConfig config in cfgs)
            {
                if (config.ForeignFields.Count > 0)
                {
                    DBHelpers.CreateForeignKeys(DBAccess.Connection, config);
                }
            }
        }
        static IConnectionInterface sqlInterface = new SqliteConnectionInterface();
        public static void ExecuteDBCode(Action<IConnectionInterface> func)
        {
            try
            {
                lck.Wait();
                func.Invoke(sqlInterface);
            }
            finally
            {
                lck.Release();
            }
        }
        public static void Initialize()
        {
            lck.Wait();
            try
            {
                if (Conn == null)
                {
                    Conn = new SQLiteConnection(GetDbSource());
                    Conn.Open();
                    CreateModels();
                }
            }
            finally
            {
                lck.Release();
            }
        }
    }
}
namespace Clippy.db.functions
{
    public class ClipItemDBUtility
    {
        public const string Table = "clip_items";
        public const string ID_Key = "id";
        const string listSeparator = "::;;::";
        public static readonly string[] foreignFields = [];
        public static readonly string[] fields = ["type", "data", "device", "id", "synced", "created_at", "updated_at"];
        //["name", "email", "createdAt", "updatedAt", "profilePic", "isVerified", "quotes"];
        public static DBCreateConfig GetDBConfig()
        {
            var cfg = new DBCreateConfig();
            cfg.TableName = Table;
            cfg.Fields.Add(new DBField
            {
                Name = "type",
                IsNullable = false,
                IsPrimaryKey = false,
                Type = "long"
            });
            cfg.Fields.Add(new DBField
            {
                Name = "data",
                IsNullable = false,
                IsPrimaryKey = false,
                Type = "string"
            });
            cfg.Fields.Add(new DBField
            {
                Name = "device",
                IsNullable = false,
                IsPrimaryKey = false,
                Type = "string"
            });
            cfg.Fields.Add(new DBField
            {
                Name = "id",
                IsNullable = false,
                IsPrimaryKey = true,
                Type = "string"
            });
            cfg.Fields.Add(new DBField
            {
                Name = "synced",
                IsNullable = false,
                IsPrimaryKey = false,
                Type = "bool"
            });
            cfg.Fields.Add(new DBField
            {
                Name = "created_at",
                IsNullable = false,
                IsPrimaryKey = false,
                Type = "DateTime"
            });
            cfg.Fields.Add(new DBField
            {
                Name = "updated_at",
                IsNullable = false,
                IsPrimaryKey = false,
                Type = "DateTime"
            });
            return cfg;
        }
        public static void CreateClipItemsTable()
        {
            var cfg = GetDBConfig();
            DBHelpers.CreateTableFromModel(DBAccess.Connection, cfg);
        }
        public static int ImportClipItems(string json)
        {
            var result = 0;
            var list = JsonConvert.DeserializeObject<List<ClipItem>>(json);
            List<string> keys = [.. fields, ID_Key];
            keys = keys.Where(x => !foreignFields.Contains(x)).Distinct().ToList();
            var keysStr = string.Join(",", keys);
            var valueStr = string.Join(",", keys.Select(e => $"@{e}"));
            var sql = $"""
										INSERT INTO {Table} ({keysStr}) VALUES ( {valueStr}  );
										""";
            DBAccess.ExecuteDBCode((db) =>
            {
                var conn = db.Connection as SQLiteConnection;
                using var transaction = conn!.BeginTransaction();
                using var command = new SQLiteCommand { Connection = conn, Transaction = transaction };
                var affectedRow = 0;
                command.CommandText = sql;
                var key = "";
                var rows = 0;
                foreach (var model in list)
                {
                    string id = Guid.CreateVersion7().ToString();
                    key = "type";
                    command.Parameters.AddWithValue($"@{key}", model.ExType);
                    key = "data";
                    command.Parameters.AddWithValue($"@{key}", model.Data);
                    key = "device";
                    command.Parameters.AddWithValue($"@{key}", model.Device);
                    key = "synced";
                    command.Parameters.AddWithValue($"@{key}", model.Synced ? 1 : 0);
                    key = "created_at";
                    command.Parameters.AddWithValue($"@{key}", ((DateTimeOffset)model.CreatedAt).ToUnixTimeMilliseconds());
                    key = "updated_at";
                    command.Parameters.AddWithValue($"@{key}", ((DateTimeOffset)model.UpdatedAt).ToUnixTimeMilliseconds());
                    key = ID_Key;
                    command.Parameters.AddWithValue($"@{key}", id);
                    try
                    {
                        rows += command.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {
                    }
                }
                transaction.Commit();
                result = rows;
            });
            return result;
        }
        public static void ExportClipItems(string file)
        {
            using StreamWriter stream = new StreamWriter(file);
            using JsonTextWriter jsonWriter = new JsonTextWriter(stream);
            jsonWriter.Formatting = Formatting.Indented;
            jsonWriter.WriteStartArray();
            JObject obj = new JObject();
            FindClipItems(onClipItem: (model) =>
            {
                obj = model.ToJSON();
                obj.WriteTo(jsonWriter);
            });
            jsonWriter.WriteEndArray();
        }
        public static ClipItem? CreateClipItem(ClipItem model, Action<string>? onError = null)
        {
            List<string> keys = [.. fields];
            keys = keys.Where(x => !foreignFields.Contains(x)).Distinct().ToList();
            //keys = keys.Where(x => x != "id").ToList();//int create bind
            ClipItem? result = null;
            var keysStr = string.Join(",", keys);
            var valueStr = string.Join(",", keys.Select(e => $"@{e}"));
            var sql = $"""
												INSERT INTO {Table} ({keysStr}) VALUES ( {valueStr}  );
												""";
            string id = Guid.CreateVersion7().ToString();
            model.Id = id;//set if string
            DBAccess.ExecuteDBCode((db) =>
            {
                var conn = db.Connection as SQLiteConnection;
                using var command = conn.CreateCommand();
                command.CommandText = sql;
                var key = "";
                keys = keys.Where(x => x != "id").ToList();//int create bind
                key = "type";
                command.Parameters.AddWithValue($"@{key}", model.ExType);
                key = "data";
                command.Parameters.AddWithValue($"@{key}", model.Data);
                key = "device";
                command.Parameters.AddWithValue($"@{key}", model.Device);
                key = "synced";
                command.Parameters.AddWithValue($"@{key}", model.Synced ? 1 : 0);
                key = "created_at";
                command.Parameters.AddWithValue($"@{key}", ((DateTimeOffset)model.CreatedAt).ToUnixTimeMilliseconds());
                key = "updated_at";
                command.Parameters.AddWithValue($"@{key}", ((DateTimeOffset)model.UpdatedAt).ToUnixTimeMilliseconds());
                key = ID_Key;
                command.Parameters.AddWithValue($"@{key}", id);//create bind id
                var rows = command.ExecuteNonQuery();
                if (rows > 0)
                {
                    //command.CommandText = "select last_insert_rowid();";
                    //id = (long)command.ExecuteScalar();
                    command.CommandText = $"select * from {Table} where {ID_Key} = @{ID_Key} limit 1;";
                    command.Parameters.AddWithValue($"@{ID_Key}", id);
                    using var reader = command.ExecuteReader();
                    if (reader != null && reader.Read())
                    {
                        key = "type";
                        model.ExType = (ClipType)((long)reader[key]);
                        key = "data";
                        model.Data = (string)reader[key];
                        key = "device";
                        model.Device = (string)reader[key];
                        key = "id";
                        model.Id = (string)reader[key];
                        key = "synced";
                        model.Synced = ((long)reader[key])>0;
                        key = "created_at";
                        model.CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)reader[key]).DateTime; ;
                        key = "updated_at";
                        model.UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)reader[key]).DateTime; ;
                        key = ID_Key;
                        model.Id = (string)reader[key];
                        result = model;
                    }
                }
                else
                {
                    if (onError != null)
                    {
                        var err = $"Failed to insert clipItem with Id '{id}'";
                        onError(err);
                    }
                }
            });
            return result;
        }
        public static bool UpdateClipItem(ClipItem model)
        {
            bool result = false;
            List<string> keys = [.. fields];
            keys = keys.Where(x => !foreignFields.Contains(x) && x != ID_Key).Distinct().ToList();
            var valueStr = string.Join(", ", keys.Select(e => $"{e} = @{e}"));
            var sql = $"""
													UPDATE {Table} SET {valueStr}  WHERE {ID_Key} =  @{ID_Key} ;
													""";
            DBAccess.ExecuteDBCode((db) =>
            {
                var conn = db.Connection as SQLiteConnection;
                using var command = conn!.CreateCommand();
                command.CommandText = sql;
                model.UpdatedAt = DateTime.Now;
                var key = "";
                key = "type";
                command.Parameters.AddWithValue($"@{key}", model.ExType);
                key = "data";
                command.Parameters.AddWithValue($"@{key}", model.Data);
                key = "device";
                command.Parameters.AddWithValue($"@{key}", model.Device);
                key = "synced";
                command.Parameters.AddWithValue($"@{key}", model.Synced ? 1 : 0);
                key = "created_at";
                command.Parameters.AddWithValue($"@{key}", ((DateTimeOffset)model.CreatedAt).ToUnixTimeMilliseconds());
                key = "updated_at";
                command.Parameters.AddWithValue($"@{key}", ((DateTimeOffset)model.UpdatedAt).ToUnixTimeMilliseconds());
                key = ID_Key;
                command.Parameters.AddWithValue($"@{key}", model.Id);
                var rows = command.ExecuteNonQuery();
                result = rows > 0;
            });
            return result;
        }
        public static int UpdateClipItems(params ClipItem[] models)
        {
            int affectedRows = 0;
            List<string> keys = [.. fields];
            keys = keys.Where(x => !foreignFields.Contains(x) && x != ID_Key).Distinct().ToList();
            var valueStr = string.Join(", ", keys.Select(e => $"{e} = @{e}"));
            var sql = $"""
														UPDATE {Table} SET {valueStr}  WHERE {ID_Key} =  @{ID_Key} ;
														""";
            DBAccess.ExecuteDBCode((db) =>
            {
                var conn = db.Connection as SQLiteConnection;
                using var transaction = conn!.BeginTransaction();
                using var command = new SQLiteCommand { Connection = conn, Transaction = transaction };
                var affectedRow = 0;
                command.CommandText = sql;
                foreach (var model in models)
                {
                    model.UpdatedAt = DateTime.Now;
                    var key = "";
                    key = "type";
                    command.Parameters.AddWithValue($"@{key}", model.ExType);
                    key = "data";
                    command.Parameters.AddWithValue($"@{key}", model.Data);
                    key = "device";
                    command.Parameters.AddWithValue($"@{key}", model.Device);
                    key = "synced";
                    command.Parameters.AddWithValue($"@{key}", model.Synced ? 1 : 0);
                    key = "created_at";
                    command.Parameters.AddWithValue($"@{key}", ((DateTimeOffset)model.CreatedAt).ToUnixTimeMilliseconds());
                    key = "updated_at";
                    command.Parameters.AddWithValue($"@{key}", ((DateTimeOffset)model.UpdatedAt).ToUnixTimeMilliseconds());
                    key = ID_Key;
                    command.Parameters.AddWithValue($"@{key}", model.Id);
                    affectedRow = command.ExecuteNonQuery();
                    affectedRows += affectedRow;
                }
                transaction.Commit();
            });
            return affectedRows;
        }
        public static DBResult<ClipItem> FindClipItems(int limit = 0, int page = 1, string orderKey = "", bool orderDescending = true, Action<ClipItem>? onClipItem = null, Query? queryParam = null, string[] loadfields = null)
        {
            var result = new DBResult<ClipItem>();
            if (queryParam != null)
            {
                queryParam = DBHelpers.NormalizeQuery(ref queryParam);
            }
            List<string> keys = loadfields != null ? [.. loadfields] : [.. fields];
            keys = keys.Where(x => !foreignFields.Contains(x)).Distinct().ToList();
            var loadKeysStr = string.Join(", ", keys);
            var sql = $"SELECT {loadKeysStr} FROM {Table} ";
            var conditions = "";
            if (queryParam != null && (queryParam.Q != null || queryParam.Queries.Count > 0))
            {
                conditions = DBHelpers.GetQueryLine(queryParam);
            }
            if (conditions.Length > 0)
            {
                sql += $" where {conditions} ";
            }
            if (orderKey.Length > 0 && (fields.Contains(orderKey) || orderKey == ID_Key))
            {
                sql += $" ORDER BY {orderKey} " + (orderDescending ? "DESC" : "ASC") + " ";
            }
            else
            {
                sql += $" ORDER BY {ID_Key} " + (orderDescending ? "DESC" : "ASC") + " ";
            }
            if (limit > 0)
            {
                if (page > 1)
                {
                    sql += $" limit {page * limit},{limit} ";
                }
                else
                {
                    sql += $" limit {limit} ";
                }
            }
            DBAccess.ExecuteDBCode((db) =>
            {
                var conn = db.Connection as SQLiteConnection;
                using var command = conn!.CreateCommand();
                command.CommandText = sql;
                if (queryParam != null)
                {
                    DBHelpers.BindQueries(command, queryParam);
                }
                using var reader = command.ExecuteReader();
                var key = "";
                while (reader.Read())
                {
                    var model = new ClipItem();
                    key = "type";
                    if (fields.Contains(key))
                    {
                        model.ExType = (ClipType)((long)reader[key]);
                    }
                    key = "data";
                    if (fields.Contains(key))
                    {
                        model.Data = (string)reader[key];
                    }
                    key = "device";
                    if (fields.Contains(key))
                    {
                        model.Device = (string)reader[key];
                    }
                    key = "id";
                    if (fields.Contains(key))
                    {
                        model.Id = (string)reader[key];
                    }
                    key = "synced";
                    if (fields.Contains(key))
                    {
                        model.Synced = ((long)reader[key])>0;
                    }
                    key = "created_at";
                    if (fields.Contains(key))
                    {
                        model.CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)reader[key]).DateTime; ;
                    }
                    key = "updated_at";
                    if (fields.Contains(key))
                    {
                        model.UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)reader[key]).DateTime; ;
                    }
                    model.Id = (string)reader[ID_Key];
                    result.Entries.Add(model);
                    onClipItem?.Invoke(model);
                }
                reader.Close();
                var countSql = $"SELECT COUNT( {ID_Key} )  FROM {Table} {(conditions.Length > 0 ? "where" : "")} {conditions};";
                command.CommandText = countSql;
                DBHelpers.BindQueries(command, queryParam);
                var total = (long)(command.ExecuteScalar());
                result.TotalCount = total;
                result.Total = result.Entries.Count;
            });
            result.Page = page;
            if (result.TotalCount > 0 && limit > 0)
            {
                result.TotalPages = (int)Math.Ceiling((decimal)result.TotalCount / limit);
            }
            return result;
        }
        public static ClipItem? FindClipItem(string id, string[] loadFields = null)
        {
            var q = new QueryParam
            {
                EQUALITY = DBEQUALITY.EQUAL,
                Value = id,
                Key = ID_Key,
            };
            var res = FindClipItems(1, 1, queryParam: new AndQuery
            {
                Queries = [q]
            }, loadfields: loadFields);
            return res.Entries.FirstOrDefault();
        }
        public static bool DeleteClipItem(string id)
        {
            bool result = false;
            DBAccess.ExecuteDBCode((db) =>
            {
                var sql = $"DELETE FROM {Table} WHERE {ID_Key} = @{ID_Key};";
                var q = new QueryParam
                {
                    EQUALITY = DBEQUALITY.EQUAL,
                    Value = id,
                    Key = ID_Key,
                };
                var conn = db.Connection as SQLiteConnection;
                using var command = conn!.CreateCommand();
                command.CommandText = sql;
                DBHelpers.BindQueries(command, new AndQuery { Queries = [q] });
                var rows = command.ExecuteNonQuery();
                result = rows > 0;
            });
            return result;
        }
        public static bool DeleteClipItem(ClipItem model)
        {
            return DeleteClipItem(model.Id);
        }
        public static int DeleteManyClipItems(params ClipItem[] models)
        {
            if (models.Length == 0) return 0;
            return DeleteManyClipItems(models.Select(e => e.Id).ToArray());
        }
        public static int DeleteManyClipItems(params string[] ids)
        {
            var result = 0;
            int affectedRows = 0;
            var sql = $"DELETE FROM {Table} WHERE {ID_Key} = @{ID_Key};";
            DBAccess.ExecuteDBCode((db) =>
            {
                var q = new QueryParam
                {
                    EQUALITY = DBEQUALITY.EQUAL,
                    Key = ID_Key,
                };
                var qParam = new AndQuery { Queries = [q] };
                var conn = db.Connection as SQLiteConnection;
                using var transaction = conn!.BeginTransaction();
                using var command = new SQLiteCommand { Connection = conn, Transaction = transaction };
                var affectedRow = 0;
                command.CommandText = sql;
                foreach (var id in ids)
                {
                    qParam.Queries[0].Value = id;
                    DBHelpers.BindQueries(command, qParam);
                    affectedRow = command.ExecuteNonQuery();
                    affectedRows += affectedRow;
                }
                transaction.Commit();
                result = affectedRows;
            });
            return result;
        }
    }
}