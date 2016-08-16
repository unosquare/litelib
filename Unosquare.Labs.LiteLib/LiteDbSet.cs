namespace Unosquare.Labs.LiteLib
{
    using Dapper;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a ILiteDbSet implementation 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Unosquare.Labs.LiteLib.ILiteDbSet{T}" />
    public class LiteDbSet<T> : ILiteDbSet<T>
        where T : ILiteModel, new()
    {

        #region Private Declarations

        private const string IntegerAffinity = "INTEGER";
        private const string NumericAffinity = "NUMERIC";
        private const string TextAffinity = "TEXT";
        private const string DateTimeAffinity = "DATETIME";

        private static readonly Dictionary<Type, string> TypeMappings = new Dictionary<Type, string>
        {
            {typeof (Int16), IntegerAffinity},
            {typeof (Int32), IntegerAffinity},
            {typeof (Int64), IntegerAffinity},
            {typeof (UInt16), IntegerAffinity},
            {typeof (UInt32), IntegerAffinity},
            {typeof (UInt64), IntegerAffinity},
            {typeof (byte), IntegerAffinity},
            {typeof (char), IntegerAffinity},
            {typeof (Decimal), NumericAffinity},
            {typeof (Boolean), NumericAffinity},
            {typeof (DateTime), DateTimeAffinity},
        };

        private class DefinitionCacheItem
        {
            public string TableName { get; set; }
            public string TableDefinition { get; set; }
            public string SelectDefinition { get; set; }
            public string InsertDefinition { get; set; }
            public string UpdateDefinition { get; set; }
            public string DeleteDefinition { get; set; }
        }

        private static readonly ConcurrentDictionary<Type, DefinitionCacheItem> DefinitionCache =
            new ConcurrentDictionary<Type, DefinitionCacheItem>();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [on before insert].
        /// </summary>
        public event EventHandler<EntityEventArgs<T>> OnBeforeInsert = (s, e) => { };

        /// <summary>
        /// Occurs when [on after insert].
        /// </summary>
        public event EventHandler<EntityEventArgs<T>> OnAfterInsert = (s, e) => { };

        /// <summary>
        /// Occurs when [on before update].
        /// </summary>
        public event EventHandler<EntityEventArgs<T>> OnBeforeUpdate = (s, e) => { };

        /// <summary>
        /// Occurs when [on after update].
        /// </summary>
        public event EventHandler<EntityEventArgs<T>> OnAfterUpdate = (s, e) => { };

        /// <summary>
        /// Occurs when [on before delete].
        /// </summary>
        public event EventHandler<EntityEventArgs<T>> OnBeforeDelete = (s, e) => { };

        /// <summary>
        /// Occurs when [on after delete].
        /// </summary>
        public event EventHandler<EntityEventArgs<T>> OnAfterDelete = (s, e) => { };


        #endregion

        #region Properties

        /// <summary>
        /// Gets the select command definition.
        /// </summary>
        public string SelectDefinition { get; protected set; }

        /// <summary>
        /// Gets the insert command definition.
        /// </summary>
        public string InsertDefinition { get; protected set; }

        /// <summary>
        /// Gets the update command definition.
        /// </summary>
        public string UpdateDefinition { get; protected set; }

        /// <summary>
        /// Gets the delete command definition.
        /// </summary>
        public string DeleteDefinition { get; protected set; }

        /// <summary>
        /// Gets the table definition.
        /// </summary>
        public string TableDefinition { get; protected set; }

        /// <summary>
        /// Gets the name of the data-backing table.
        /// </summary>
        public string TableName { get; protected set; }

        /// <summary>
        /// Gets or sets the parent set context.
        /// </summary>
        public LiteDbContext Context { get; set; }

        /// <summary>
        /// Gets or sets the type of the entity.
        /// </summary>
        public Type EntityType { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbSet{T}"/> class.
        /// </summary>
        public LiteDbSet()
        {
            LoadDefinitions();
        }

        #endregion

        #region Methods and Data Access

        /// <summary>
        /// Loads the necessary command definitions to create a backing table and
        /// perform CRUD operations with models.
        /// </summary>
        private void LoadDefinitions()
        {
            var dbSetType = GetType();
            if (DefinitionCache.ContainsKey(dbSetType))
            {
                var cache = DefinitionCache[dbSetType];
                TableName = cache.TableName;
                TableDefinition = cache.TableDefinition;
                SelectDefinition = cache.SelectDefinition;
                InsertDefinition = cache.InsertDefinition;
                UpdateDefinition = cache.UpdateDefinition;
                DeleteDefinition = cache.DeleteDefinition;
                return;
            }

            var createBuilder = new StringBuilder();
            var indexBuilder = new List<string>();

            var publicInstanceFlags = BindingFlags.Instance | BindingFlags.Public;
            var properties = typeof (T).GetProperties(publicInstanceFlags);
            var propertyNames = new List<string>();

            // Start off with the table name
            var tableName = nameof(T);
            var tableAttribute = Attribute.GetCustomAttribute(typeof (T), typeof (TableAttribute)) as TableAttribute;
            if (tableAttribute != null)
                tableName = tableAttribute.Name;

            createBuilder.AppendLine($"CREATE TABLE IF NOT EXISTS [{tableName}] (");
            createBuilder.AppendLine($"    [{nameof(ILiteModel.RowId)}] INTEGER PRIMARY KEY AUTOINCREMENT,");

            foreach (var property in properties)
            {
                if (property.Name == nameof(ILiteModel.RowId))
                    continue;

                if (property.CanWrite == false)
                    continue;

                {
                    // Skip if not mapped
                    var notMappedAttribute =
                        Attribute.GetCustomAttribute(property, typeof (NotMappedAttribute)) as NotMappedAttribute;
                    if (notMappedAttribute != null)
                        continue;
                }

                {
                    // Add to indexes if indexed attribute is ON
                    var indexedAttribute =
                        Attribute.GetCustomAttribute(property, typeof (LiteIndexAttribute)) as LiteIndexAttribute;
                    if (indexedAttribute != null)
                    {
                        indexBuilder.Add(
                            $"CREATE INDEX IF NOT EXISTS [IX_{tableName}_{property.Name}] ON [{tableName}] ([{property.Name}]);");
                    }
                }

                {
                    // Add to unique indexes if indexed attribute is ON
                    var uniqueIndexAttribute =
                        Attribute.GetCustomAttribute(property, typeof (LiteUniqueAttribute)) as LiteUniqueAttribute;
                    if (uniqueIndexAttribute != null)
                    {
                        indexBuilder.Add(
                            $"CREATE UNIQUE INDEX IF NOT EXISTS [IX_{tableName}_{property.Name}] ON [{tableName}] ([{property.Name}]);");
                    }
                }

                propertyNames.Add(property.Name);
                var propertyType = property.PropertyType;
                var isNullable = propertyType.IsGenericType &&
                                 propertyType.GetGenericTypeDefinition() == typeof (Nullable<>);
                if (isNullable) propertyType = Nullable.GetUnderlyingType(propertyType);
                var nullStatement = isNullable ? "NULL" : "NOT NULL";

                if (propertyType == typeof (string))
                {
                    var stringLength = 4096;
                    {
                        var stringLengthAttribute =
                            Attribute.GetCustomAttribute(property, typeof (StringLengthAttribute)) as
                                StringLengthAttribute;
                        if (stringLengthAttribute != null)
                        {
                            stringLength = stringLengthAttribute.MaximumLength;
                        }
                    }

                    isNullable =
                        (Attribute.GetCustomAttribute(property, typeof (RequiredAttribute)) as RequiredAttribute == null);
                    nullStatement = isNullable ? "NULL" : "NOT NULL";
                    if (stringLength != 4096)
                    {
                        var checkLength = $"length({property.Name})<={stringLength}";
                        createBuilder.AppendLine(
                            $"    [{property.Name}] NVARCHAR({stringLength}) {nullStatement} CHECK({checkLength}),");
                    }
                    else
                    {
                        createBuilder.AppendLine($"    [{property.Name}] NVARCHAR({stringLength}) {nullStatement},");
                    }

                }
                else if (propertyType.IsValueType)
                {
                    if (TypeMappings.ContainsKey(propertyType))
                    {
                        createBuilder.AppendLine($"    [{property.Name}] {TypeMappings[propertyType]} {nullStatement},");
                    }
                    else
                    {
                        createBuilder.AppendLine($"    [{property.Name}] TEXT {nullStatement},");
                    }
                }
                else if (propertyType == typeof (byte[]))
                {
                    createBuilder.AppendLine($"    [{property.Name}] BLOB {nullStatement},");
                }
            }

            //trim out the extra comma
#if MONO
            createBuilder.Remove(createBuilder.Length - 2, 2);
#else
            createBuilder.Remove(createBuilder.Length - 3, 3);
#endif
            createBuilder.AppendLine();
            createBuilder.AppendLine($");");

            foreach (var indexDdl in indexBuilder)
            {
                createBuilder.AppendLine(indexDdl);
            }

            var escapedColumnNames = string.Join(", ", propertyNames.Select(p => $"[{p}]").ToArray());
            var parameterColumnNames = string.Join(", ", propertyNames.Select(p => $"@{p}").ToArray());
            var keyValueColumnNames = string.Join(", ", propertyNames.Select(p => $"[{p}] = @{p}").ToArray());

            TableName = tableName;
            TableDefinition = createBuilder.ToString();
            SelectDefinition = $"SELECT [{nameof(ILiteModel.RowId)}], {escapedColumnNames} FROM [{tableName}]";
            InsertDefinition =
                $"INSERT INTO [{tableName}] ({escapedColumnNames}) VALUES ({parameterColumnNames}); SELECT last_insert_rowid();";
            UpdateDefinition =
                $"UPDATE [{tableName}] SET {keyValueColumnNames}  WHERE [{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}";
            DeleteDefinition =
                $"DELETE FROM [{tableName}] WHERE [{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}";

            DefinitionCache[dbSetType] = new DefinitionCacheItem
            {
                TableName = TableName,
                TableDefinition = TableDefinition,
                SelectDefinition = SelectDefinition,
                InsertDefinition = InsertDefinition,
                UpdateDefinition = UpdateDefinition,
                DeleteDefinition = DeleteDefinition
            };
        }

        /// <summary>
        /// Logs the SQL command being executed and its arguments.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="arguments">The arguments.</param>
        private void LogSqlCommand(string command, object arguments)
        {
            if (Debugger.IsAttached == false) return;
            if (Context?.Logger == null) return;

            //var task = Task.Factory.StartNew(() => {
            var argumentsText = new StringBuilder();

            if (arguments != null)
            {
                argumentsText.AppendLine();
                argumentsText.AppendLine("  {");
                var props = arguments.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var prop in props)
                {
                    if (!prop.CanRead) continue;
                    object propValue = null;

                    try
                    {
                        propValue = prop.GetValue(arguments);
                    }
                    catch
                    {
                        // ignored
                    }
                    argumentsText.AppendLine($"    \"{prop.Name}\": \"{propValue ?? ""}\"");
                }

                argumentsText.AppendLine("  }");
            }

            Context.Logger.DebugFormat($"> {command}{argumentsText}");
            //});
        }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Insert(T entity)
        {
            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeInsert(this, args);
            if (args.Cancel) return 0;

            LogSqlCommand(InsertDefinition, entity);
            entity.RowId = Context.Connection.Query<long>(InsertDefinition, entity).FirstOrDefault();
            OnAfterInsert(this, args);
            return 1;
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Insert method
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public async Task<int> InsertAsync(T entity)
        {
            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeInsert(this, args);
            if (args.Cancel) return 0;

            LogSqlCommand(InsertDefinition, entity);
            var result = await Context.Connection.QueryAsync<long>(InsertDefinition, entity);

            if (result.Any())
            {
                entity.RowId = result.First();
                OnAfterInsert(this, args);
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Deletes the specified entity. RowId must be set.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public int Delete(T entity)
        {
            if (entity.RowId == default(long))
                throw new ArgumentException(nameof(entity.RowId));

            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeDelete(this, args);
            if (args.Cancel) return 0;

            LogSqlCommand(DeleteDefinition, entity);
            var affected = Context.Connection.Execute(DeleteDefinition, entity);
            entity.RowId = default(long);
            OnAfterDelete(this, args);
            return affected;
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Delete method
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task<int> DeleteAsync(T entity)
        {
            if (entity.RowId == default(long))
                throw new ArgumentException(nameof(entity.RowId));

            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeDelete(this, args);
            if (args.Cancel) return 0;

            LogSqlCommand(DeleteDefinition, entity);
            var affected = await Context.Connection.ExecuteAsync(DeleteDefinition, entity);
            entity.RowId = default(long);
            OnAfterDelete(this, args);

            return affected;
        }

        /// <summary>
        /// Updates the specified entity in a non optimistic concurrency manner.
        /// RowId must be set.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Update(T entity)
        {
            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeUpdate(this, args);
            if (args.Cancel) return 0;

            LogSqlCommand(UpdateDefinition, entity);
            var affected = Context.Connection.Execute(UpdateDefinition, entity);
            OnAfterUpdate(this, args);
            return affected;
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Update method
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(T entity)
        {
            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeUpdate(this, args);
            if (args.Cancel) return 0;

            LogSqlCommand(UpdateDefinition, entity);
            var affected = await Context.Connection.ExecuteAsync(UpdateDefinition, entity);

            OnAfterUpdate(this, args);
            return affected;
        }

        /// <summary>
        /// Selects a set of entities from the database.
        /// Example whereText = "X = @X" and whereParames = new { X = "hello" }
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns></returns>
        public IEnumerable<T> Select(string whereText, object whereParams = null)
        {
            var commandText = $"{SelectDefinition} WHERE {whereText}";
            LogSqlCommand(commandText, whereParams);
            return Context.Connection.Query<T>(commandText, whereParams);
        }

        /// <summary>
        /// Selects all entities from the database.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> SelectAll()
        {
            return Select("1 = 1");
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Select method
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> SelectAsync(string whereText, object whereParams)
        {
            var commandText = $"{SelectDefinition} WHERE {whereText}";
            LogSqlCommand(commandText, whereParams);
            return await Context.Connection.QueryAsync<T>(commandText, whereParams);
        }

        /// <summary>
        /// Selects all asynchronous.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<T>> SelectAllAsync()
        {
            return await SelectAsync("1 = 1", null);
        }

        /// <summary>
        /// Selects a single entity from the database given its row id.
        /// </summary>
        /// <param name="rowId">The row identifier.</param>
        /// <returns></returns>
        public T Single(long rowId)
        {
            return
                Select($"[{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}", new {RowId = rowId})
                    .FirstOrDefault();
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Single method
        /// </summary>
        /// <param name="rowId">The row identifier.</param>
        /// <returns></returns>
        public async Task<T> SingleAsync(long rowId)
        {
            var result =
                await SelectAsync($"[{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}", new {RowId = rowId});
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Counts the total number of rows in the table
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            var commandText = $"SELECT COUNT(*) FROM [{TableName}]";
            LogSqlCommand(commandText, null);
            return Context.Connection.ExecuteScalar<int>(commandText);
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Count method
        /// </summary>
        /// <returns></returns>
        public async Task<int> CountAsync()
        {
            var commandText = $"SELECT COUNT(*) FROM [{TableName}]";
            LogSqlCommand(commandText, null);
            return await Context.Connection.ExecuteScalarAsync<int>(commandText);
        }

        /// <summary>
        /// Performs a custom query. The command text and parameters are NOT automatically provided.
        /// Use the SelectDefinition property to start with command text.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandParams">The command parameters.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerable<T> Query(string commandText, object commandParams)
        {
            LogSqlCommand(commandText, commandParams);
            return Context.Connection.Query<T>(commandText, commandParams);
        }

        /// <summary>
        /// Provides an asynchronous counterpart to the Query method
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandParams">The command parameters.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task<IEnumerable<T>> QueryAsync(string commandText, object commandParams)
        {
            LogSqlCommand(commandText, commandParams);
            return await Context.Connection.QueryAsync<T>(commandText, commandParams);
        }

#endregion
    }
}