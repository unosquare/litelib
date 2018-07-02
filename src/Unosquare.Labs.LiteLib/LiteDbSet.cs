namespace Unosquare.Labs.LiteLib
{
    using Dapper;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a ILiteDbSet implementation
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <seealso cref="LiteLib.ILiteDbSet{T}" />
    public class LiteDbSet<T> : ILiteDbSet<T>
        where T : ILiteModel, new()
    {
        private const BindingFlags PublicInstanceFlags = BindingFlags.Instance | BindingFlags.Public;

        private static readonly ConcurrentDictionary<Type, DefinitionCacheItem> DefinitionCache =
            new ConcurrentDictionary<Type, DefinitionCacheItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbSet{T}"/> class.
        /// </summary>
        public LiteDbSet()
        {
            LoadDefinitions();
        }

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

        #endregion Events

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
        /// Gets the delete definition where.
        /// </summary>
        public string DeleteDefinitionWhere { get; protected set; }

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

        /// <summary>
        /// Gets or sets the property names.
        /// </summary>
        public string[] PropertyNames { get; set; }

        #endregion Properties

        #region Methods and Data Access

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// The number of rows inserted
        /// </returns>
        public int Insert(T entity)
        {
            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeInsert(this, args);
            if (args.Cancel) return 0;

            Context.LogSqlCommand(InsertDefinition, entity);
            entity.RowId = Context.Connection.Query<long>(InsertDefinition, entity).FirstOrDefault();
            OnAfterInsert(this, args);
            return 1;
        }

        /// <summary>
        /// Inserts the specified entities.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <exception cref="ArgumentNullException">entities</exception>
        public void InsertRange(IEnumerable<T> entities)
        {
            if (entities == null || entities.Any() == false)
                throw new ArgumentNullException(nameof(entities));

            var escapedColumnNames = string.Join(", ", PropertyNames.Select(p => $"[{p}]").ToArray());
            var command = $"INSERT INTO [{TableName}] ({escapedColumnNames})";
            var baseTypeProperties =
                typeof(T).GetTypeInfo().GetProperties().Where(x => PropertyNames.Contains(x.Name)).ToArray();

            command += string.Join("UNION ALL ",
                entities.Select(
                    entity => "SELECT " + string.Join(", ",
                                  baseTypeProperties.Select(p => $"'{p.GetValue(entity)}'"))));

            Context.Connection.ExecuteScalar(command);
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Insert method
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// A Task with the number of rows inserted
        /// </returns>
        public async Task<int> InsertAsync(T entity)
        {
            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeInsert(this, args);
            if (args.Cancel) return 0;

            Context.LogSqlCommand(InsertDefinition, entity);
            var result = await Context.Connection.QueryAsync<long>(InsertDefinition, entity);

            if (result.Any() == false) return 0;

            entity.RowId = result.First();
            OnAfterInsert(this, args);
            return 1;
        }

        /// <summary>
        /// Deletes the specified where text.
        /// Example whereText = "X = @X" and whereParames = new { X = "hello" }
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>
        /// A count of affected rows.
        /// </returns>
        public int Delete(string whereText, object whereParams = null) => Context.Delete(this, whereText, whereParams);

        /// <summary>
        /// Deletes the asynchronous.
        /// Example whereText = "X = @X" and whereParames = new { X = "hello" }.
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>
        /// A count of affected rows.
        /// </returns>
        public Task<int> DeleteAsync(string whereText, object whereParams = null) => Context.DeleteAsync(this, whereText, whereParams);

        /// <summary>
        /// Deletes the specified entity. RowId must be set.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// The number of rows deleted
        /// </returns>
        /// <exception cref="ArgumentException">RowId</exception>
        public int Delete(T entity)
        {
            if (entity.RowId == default)
                throw new ArgumentException(nameof(entity.RowId));

            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeDelete(this, args);
            if (args.Cancel) return 0;

            Context.LogSqlCommand(DeleteDefinition, entity);
            var affected = Context.Connection.Execute(DeleteDefinition, entity);
            entity.RowId = default;
            OnAfterDelete(this, args);
            return affected;
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Delete method
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// A Task with the number of rows deleted
        /// </returns>
        /// <exception cref="ArgumentException">RowId</exception>
        public async Task<int> DeleteAsync(T entity)
        {
            if (entity.RowId == default)
                throw new ArgumentException(nameof(entity.RowId));

            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeDelete(this, args);
            if (args.Cancel) return 0;

            Context.LogSqlCommand(DeleteDefinition, entity);
            var affected = await Context.Connection.ExecuteAsync(DeleteDefinition, entity);
            entity.RowId = default;
            OnAfterDelete(this, args);

            return affected;
        }

        /// <summary>
        /// Updates the specified entity in a non optimistic concurrency manner.
        /// RowId must be set.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// The number of rows updated
        /// </returns>
        public int Update(T entity)
        {
            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeUpdate(this, args);
            if (args.Cancel) return 0;

            Context.LogSqlCommand(UpdateDefinition, entity);
            var affected = Context.Connection.Execute(UpdateDefinition, entity);
            OnAfterUpdate(this, args);
            return affected;
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Update method
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// A Task with the number of rows updated
        /// </returns>
        public async Task<int> UpdateAsync(T entity)
        {
            var args = new EntityEventArgs<T>(entity, this);
            OnBeforeUpdate(this, args);
            if (args.Cancel) return 0;

            Context.LogSqlCommand(UpdateDefinition, entity);
            var affected = await Context.Connection.ExecuteAsync(UpdateDefinition, entity);
            OnAfterUpdate(this, args);
            return affected;
        }

        /// <summary>
        /// Selects a set of entities from the database.
        /// Example whereText = "X = @X" and whereParames = new { X = "hello" }.
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>
        /// An Enumerable with generic type
        /// </returns>
        public IEnumerable<T> Select(string whereText, object whereParams = null) => Context.Select<T>(this, whereText, whereParams);

        /// <summary>
        /// Selects all entities from the database.
        /// </summary>
        /// <returns>
        /// An Enumerable with generic type.
        /// </returns>
        public IEnumerable<T> SelectAll() => Select("1 = 1");

        /// <summary>
        /// Provides and asynchronous counterpart to the Select method.
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>
        /// A Task of type Enumerable with a generic type.
        /// </returns>
        public Task<IEnumerable<T>> SelectAsync(string whereText, object whereParams = null) => Context.SelectAsync<T>(this, whereText, whereParams);

        /// <summary>
        /// Selects all asynchronous.
        /// </summary>
        /// <returns>
        /// A Task of type Enumerable with a generic type.
        /// </returns>
        public Task<IEnumerable<T>> SelectAllAsync() => SelectAsync("1 = 1");

        /// <summary>
        /// Firsts the or default.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <returns> A generic type</returns>
        public T FirstOrDefault(string fieldName, object fieldValue) => Select($"[{fieldName}] = @FieldValue", new { FieldValue = fieldValue }).FirstOrDefault();

        /// <summary>
        /// Firsts the or default asynchronous.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <returns>A Task with a generic type.</returns>
        public async Task<T> FirstOrDefaultAsync(string fieldName, object fieldValue)
        {
            var result = await SelectAsync($"[{fieldName}] = @FieldValue", new { FieldValue = fieldValue });

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Selects a single entity from the database given its row id.
        /// </summary>
        /// <param name="rowId">The row identifier.</param>
        /// <returns>
        /// A generic type.
        /// </returns>
        public T Single(long rowId) => Select($"[{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}", new { RowId = rowId })
            .FirstOrDefault();

        /// <summary>
        /// Provides and asynchronous counterpart to the Single method
        /// </summary>
        /// <param name="rowId">The row identifier.</param>
        /// <returns>
        /// A Task with a generyc type.
        /// </returns>
        public async Task<T> SingleAsync(long rowId)
        {
            var result =
                await SelectAsync($"[{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}", new { RowId = rowId });
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Counts the total number of rows in the table
        /// </summary>
        /// <returns>
        /// The total number of rows
        /// </returns>
        public int Count()
        {
            var commandText = $"SELECT COUNT(*) FROM [{TableName}]";
            Context.LogSqlCommand(commandText);
            return Context.Connection.ExecuteScalar<int>(commandText);
        }

        /// <summary>
        /// Provides and asynchronous counterpart to the Count method
        /// </summary>
        /// <returns>
        /// A Task with the total number of rows
        /// </returns>
        public async Task<int> CountAsync()
        {
            var commandText = $"SELECT COUNT(*) FROM [{TableName}]";
            Context.LogSqlCommand(commandText);
            return await Context.Connection.ExecuteScalarAsync<int>(commandText);
        }

        /// <summary>
        /// Loads the necessary command definitions to create a backing table and
        /// perform CRUD operations with models.
        /// </summary>
        private void LoadDefinitions()
        {
            var setType = GetType();

            if (DefinitionCache.ContainsKey(setType))
            {
                var cache = DefinitionCache[setType];
                TableName = cache.TableName;
                TableDefinition = cache.TableDefinition;
                SelectDefinition = cache.SelectDefinition;
                InsertDefinition = cache.InsertDefinition;
                UpdateDefinition = cache.UpdateDefinition;
                DeleteDefinition = cache.DeleteDefinition;
                DeleteDefinitionWhere = cache.DeleteDefinitionWhere;
                PropertyNames = cache.PropertyNames;

                return;
            }

            var createBuilder = new StringBuilder();
            var indexBuilder = new List<string>();

            var properties = typeof(T).GetProperties(PublicInstanceFlags);
            var propertyNames = new List<string>();

            // Start off with the table name
            var tableName = typeof(T).Name;
            var tableAttribute = typeof(T).GetTypeInfo().GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null)
                tableName = tableAttribute.Name;

            createBuilder.AppendLine($"CREATE TABLE IF NOT EXISTS [{tableName}] (");
            createBuilder.AppendLine($"    [{nameof(ILiteModel.RowId)}] INTEGER PRIMARY KEY AUTOINCREMENT,");

            foreach (var property in properties)
            {
                if (property.Name == nameof(ILiteModel.RowId) || property.CanWrite == false)
                    continue;

                // Skip if not mapped
                var notMappedAttribute = property.GetCustomAttribute<NotMappedAttribute>();
                if (notMappedAttribute != null)
                    continue;

                // Add to indexes if indexed attribute is ON
                var indexedAttribute = property.GetCustomAttribute<LiteIndexAttribute>();
                if (indexedAttribute != null)
                {
                    indexBuilder.Add(
                        $"CREATE INDEX IF NOT EXISTS [IX_{tableName}_{property.Name}] ON [{tableName}] ([{property.Name}]);");
                }

                // Add to unique indexes if indexed attribute is ON
                var uniqueIndexAttribute = property.GetCustomAttribute<LiteUniqueAttribute>();

                if (uniqueIndexAttribute != null)
                {
                    indexBuilder.Add(
                        $"CREATE UNIQUE INDEX IF NOT EXISTS [IX_{tableName}_{property.Name}] ON [{tableName}] ([{property.Name}]);");
                }

                GeneratePropertyInfo(property, createBuilder, propertyNames);
            }

            if (propertyNames.Any() == false)
                throw new Exception("Invalid DbSet, you need at least one property to bind");

            // trim out the extra comma
            createBuilder.Remove(createBuilder.Length - Environment.NewLine.Length - 1, Environment.NewLine.Length + 1);

            createBuilder.AppendLine();
            createBuilder.AppendLine(");");

            foreach (var indexDdl in indexBuilder)
            {
                createBuilder.AppendLine(indexDdl);
            }

            var escapedColumnNames = string.Join(", ", propertyNames.Select(p => $"[{p}]").ToArray());
            var parameterColumnNames = string.Join(", ", propertyNames.Select(p => $"@{p}").ToArray());
            var keyValueColumnNames = string.Join(", ", propertyNames.Select(p => $"[{p}] = @{p}").ToArray());

            TableName = tableName;
            TableDefinition = createBuilder.ToString();
            PropertyNames = propertyNames.ToArray();

            SelectDefinition = $"SELECT [{nameof(ILiteModel.RowId)}], {escapedColumnNames} FROM [{tableName}]";
            InsertDefinition =
                $"INSERT INTO [{tableName}] ({escapedColumnNames}) VALUES ({parameterColumnNames}); SELECT last_insert_rowid();";
            UpdateDefinition =
                $"UPDATE [{tableName}] SET {keyValueColumnNames}  WHERE [{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}";
            DeleteDefinition =
                $"DELETE FROM [{tableName}] WHERE [{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}";
            DeleteDefinitionWhere = $"DELETE FROM [{tableName}]";

            DefinitionCache[setType] = new DefinitionCacheItem
            {
                TableName = TableName,
                TableDefinition = TableDefinition,
                SelectDefinition = SelectDefinition,
                InsertDefinition = InsertDefinition,
                UpdateDefinition = UpdateDefinition,
                DeleteDefinition = DeleteDefinition,
                DeleteDefinitionWhere = DeleteDefinitionWhere,
                PropertyNames = PropertyNames
            };
        }

        private static void GeneratePropertyInfo(PropertyInfo property, StringBuilder createBuilder, List<string> propertyNames)
        {
            var isValidProperty = false;
            var propertyType = property.PropertyType;
            var isNullable = propertyType.GetTypeInfo().IsGenericType &&
                             propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable) propertyType = Nullable.GetUnderlyingType(propertyType);
            var nullStatement = isNullable ? "NULL" : "NOT NULL";

            if (propertyType == typeof(string))
            {
                isValidProperty = true;

                var stringLength = 4096;
                {
                    var stringLengthAttribute = property.GetCustomAttribute<StringLengthAttribute>();

                    if (stringLengthAttribute != null)
                    {
                        stringLength = stringLengthAttribute.MaximumLength;
                    }
                }

                isNullable = property.GetCustomAttribute<RequiredAttribute>() == null;

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
            else if (propertyType.GetTypeInfo().IsValueType)
            {
                isValidProperty = true;
                createBuilder.AppendLine(
                    $"    [{property.Name}] {propertyType.GetTypeMapping()} {nullStatement},");
            }
            else if (propertyType == typeof(byte[]))
            {
                isValidProperty = true;
                createBuilder.AppendLine($"    [{property.Name}] BLOB {nullStatement},");
            }

            if (isValidProperty)
            {
                propertyNames.Add(property.Name);
            }
        }

        #endregion Methods and Data Access

        private class DefinitionCacheItem
        {
            public string TableName { get; set; }

            public string TableDefinition { get; set; }

            public string SelectDefinition { get; set; }

            public string InsertDefinition { get; set; }

            public string UpdateDefinition { get; set; }

            public string DeleteDefinition { get; set; }

            public string DeleteDefinitionWhere { get; set; }

            public string[] PropertyNames { get; set; }
        }
    }
}