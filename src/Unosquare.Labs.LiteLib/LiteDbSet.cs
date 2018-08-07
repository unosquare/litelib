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
        private readonly DefinitionCacheItem tableDefinition;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbSet{T}"/> class.
        /// </summary>
        public LiteDbSet()
        {
            tableDefinition = new TypeDefinition(typeof(T)).Definition;
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
        public string SelectDefinition => tableDefinition.SelectDefinition;

        /// <summary>
        /// Gets the insert command definition.
        /// </summary>
        public string InsertDefinition  => tableDefinition.InsertDefinition;

        /// <summary>
        /// Gets the update command definition.
        /// </summary>
        public string UpdateDefinition => tableDefinition.UpdateDefinition;

        /// <summary>
        /// Gets the delete command definition.
        /// </summary>
        public string DeleteDefinition => tableDefinition.DeleteDefinition;

        /// <summary>
        /// Gets the delete definition where.
        /// </summary>
        public string DeleteDefinitionWhere => tableDefinition.DeleteDefinitionWhere;

        /// <summary>
        /// Gets or sets any definition.
        /// </summary>
        public string AnyDefinition => tableDefinition.AnyDefinition;

        /// <summary>
        /// Gets the table definition.
        /// </summary>
        public string TableDefinition => tableDefinition.TableDefinition;

        /// <summary>
        /// Gets the name of the data-backing table.
        /// </summary>
        public string TableName  => tableDefinition.TableName;

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
        public string[] PropertyNames  => tableDefinition.PropertyNames;

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
        /// Example whereText = "X = @X" and whereParames = new { X = "hello" }.
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
        /// The number of rows updated.
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
        /// An Enumerable with generic type.
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
        /// Provides and asynchronous counterpart to the Count method.
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>
        /// The total number of rows.
        /// </returns>
        public int Count(string whereText, object whereParams = null)
            => Context.ExecuteScalar<int>($"SELECT COUNT(*) FROM [{TableName}] WHERE {whereText})", whereParams);

        /// <summary>
        /// Counts the total number of rows in the table.
        /// </summary>
        /// <returns>
        /// The total number of rows.
        /// </returns>
        public int Count()
            => Context.ExecuteScalar<int>($"SELECT COUNT(*) FROM [{TableName}]");

        /// <summary>
        /// Provides and asynchronous counterpart to the Count method.
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>
        /// A Task with the total number of rows.
        /// </returns>
        public Task<int> CountAsync(string whereText, object whereParams = null)
            => Context.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM [{TableName}] WHERE {whereText})", whereParams);

        /// <summary>
        /// Provides and asynchronous counterpart to the Count method.
        /// </summary>
        /// <returns>
        /// A Task with the total number of rows.
        /// </returns>
        public Task<int> CountAsync()
            => Context.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM [{TableName}]");

        /// <summary>
        /// Check if the row exist in the table.
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns><c>true</c> if the query contains data, otherwise <c>false</c>.</returns>
        public bool Any(string whereText, object whereParams = null)
            => Context.ExecuteScalar<bool>($"SELECT EXISTS(SELECT 1 FROM '{TableName}' WHERE {whereText})", whereParams);

        /// <summary>
        /// Check if the row exist in the table.
        /// </summary>
        /// <returns><c>true</c> if the query contains data, otherwise <c>false</c>.</returns>
        public bool Any()
            => Context.ExecuteScalar<bool>(AnyDefinition);

        /// <summary>
        /// Check asynchronous if the row exist in the table.
        /// </summary>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<bool> AnyAsync(string whereText, object whereParams = null)
            => Context.ExecuteScalarAsync<bool>($"SELECT EXISTS(SELECT 1 FROM '{TableName}' WHERE {whereText})", whereParams);

        /// <summary>
        /// Check asynchronous if the table contains data.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<bool> AnyAsync()
            => Context.ExecuteScalarAsync<bool>(AnyDefinition);

        #endregion Methods and Data Access
    }
}