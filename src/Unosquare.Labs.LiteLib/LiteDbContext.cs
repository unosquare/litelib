namespace Unosquare.Labs.LiteLib
{
    using Dapper;
    using System.Diagnostics;
    using System.Data;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Swan;
    using Swan.Reflection;
#if MONO
    using Mono.Data.Sqlite;
#else
    using Microsoft.Data.Sqlite;
#endif

    /// <summary>
    /// A base class containing all the functionality to perform data operations on Entity Sets
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public abstract class LiteDbContext : IDisposable
    {
        #region Private Declarations 

        private readonly Dictionary<string, ILiteDbSet> _entitySets = new Dictionary<string, ILiteDbSet>();
        private readonly Type _contextType;
        private static readonly ConcurrentDictionary<Guid, LiteDbContext> Intances = new ConcurrentDictionary<Guid, LiteDbContext>();
        private static readonly PropertyTypeCache PropertyInfoCache = new PropertyTypeCache();
        private static readonly Type GenericLiteDbSetType = typeof(LiteDbSet<>);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbContext" /> class.
        /// </summary>
        /// <param name="databaseFilePath">The database file path.</param>
        /// <param name="enabledLog">if set to <c>true</c> [enabled log].</param>
        protected LiteDbContext(string databaseFilePath, bool enabledLog = true)
        {
            EnabledLog = enabledLog;
            _contextType = GetType();
            LoadEntitySets();

            databaseFilePath = Path.GetFullPath(databaseFilePath);
            var databaseExists = File.Exists(databaseFilePath);
#if MONO
            Connection = new SqliteConnection($"URI=file:{databaseFilePath}");
#else
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = databaseFilePath,
                // DateTimeKind = DateTimeKind.Utc
            };

            Connection = new SqliteConnection(builder.ToString());
#endif
            Connection.Open();

            if (databaseExists == false)
            {
                "DB file does not exist. Creating.".Debug(nameof(LiteDbContext));
                CreateDatabase();
                $"DB file created: '{databaseFilePath}'".Debug(nameof(LiteDbContext));
            }

            UniqueId = Guid.NewGuid();
            Intances[UniqueId] = this;
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [on database created].
        /// </summary>
        public event EventHandler OnDatabaseCreated = (s, e) => { };

        #endregion

        #region Methods

        /// <summary>
        /// Logs the SQL command being executed and its arguments.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="arguments">The arguments.</param>
        internal void LogSqlCommand(string command, object arguments)
        {
            if (EnabledLog == false) return;

            if (Debugger.IsAttached == false || Terminal.IsConsolePresent == false) return;

            $"> {command}{arguments.Stringify()}".Debug(nameof(LiteDbContext));
        }

        /// <summary>
        /// Loads the entity sets registered as virtual public properties of the derived class.
        /// </summary>
        private void LoadEntitySets()
        {
            var contextDbSetProperties = PropertyInfoCache.Retrieve(GetType(), () =>
            {
                return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(
                        p =>
                            p.PropertyType.GetTypeInfo().IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == GenericLiteDbSetType);
            });
            
            foreach (var entitySetProp in contextDbSetProperties)
            {
                var entitySetType = entitySetProp.PropertyType.GetGenericArguments()[0];
                var currentValue = entitySetProp.GetValue(this) as ILiteDbSet;

                if (currentValue == null)
                {
                    var instanceType = GenericLiteDbSetType.MakeGenericType(entitySetType);
                    currentValue = Activator.CreateInstance(instanceType) as ILiteDbSet;
                    entitySetProp.SetValue(this, currentValue);
                }

                if (currentValue == null) continue;

                currentValue.Context = this;
                currentValue.EntityType = entitySetType;
                _entitySets[entitySetProp.Name] = currentValue;
            }

            $"Context instance {_contextType.Name} - {_entitySets.Count} entity sets. {Instances.Count} context instances.".Debug(nameof(LiteDbContext));
        }

        /// <summary>
        /// Vacuums the database asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task VaccuumDatabaseAsync()
        {
            "DB VACUUM command executing.".Debug(nameof(LiteDbContext));
            await Connection.ExecuteAsync("VACUUM");
            "DB VACUUM command finished.".Debug(nameof(LiteDbContext));
        }

        /// <summary>
        /// Returns a non-generic ILiteDbSet instance for access to entities of the given type in the context and the underlying store.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ILiteDbSet Set(Type entityType)
        {
            var set = _entitySets.Values.FirstOrDefault(x => x.GetType().GetTypeInfo().GetGenericArguments().Any(z => z == entityType));

            if (set == null)
                throw new ArgumentOutOfRangeException(nameof(entityType));

            return set;
        }

        /// <summary>
        /// Returns a ILiteDbSet instance for access to entities of the given type in the context and the underlying store.
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns></returns>
        public ILiteDbSet Set<TEntity>()
        {
            return Set(typeof(TEntity));
        }

        /// <summary>
        /// Gets the set names.
        /// </summary>
        /// <returns></returns>
        public string[] GetSetNames() => _entitySets.Keys.ToArray();

        /// <summary>
        /// Selects the specified set.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="set">The set.</param>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns></returns>
        public IEnumerable<TEntity> Select<TEntity>(ILiteDbSet set, string whereText, object whereParams = null)
        {
            return Query<TEntity>($"{set.SelectDefinition} WHERE {whereText}", whereParams);
        }
        
        /// <summary>
        /// Selects the asynchronous.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="set">The set.</param>
        /// <param name="whereText">The where text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns></returns>
        public async Task<IEnumerable<TEntity>> SelectAsync<TEntity>(ILiteDbSet set, string whereText, object whereParams = null)
        {
            return await QueryAsync<TEntity>($"{set.SelectDefinition} WHERE {whereText}", whereParams);
        }

        /// <summary>
        /// Queries the specified set.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="commandText">The command text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>An enumerable of the type</returns>
        public IEnumerable<TEntity> Query<TEntity>(string commandText, object whereParams = null)
        {
            LogSqlCommand(commandText, whereParams);
            return Connection.Query<TEntity>(commandText, whereParams);
        }

        /// <summary>
        /// Queries the asynchronous.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="commandText">The command text.</param>
        /// <param name="whereParams">The where parameters.</param>
        /// <returns>A Task with an enumerable of the type</returns>
        public async Task<IEnumerable<TEntity>> QueryAsync<TEntity>(string commandText, object whereParams = null)
        {
            LogSqlCommand(commandText, whereParams);
            return await Connection.QueryAsync<TEntity>(commandText, whereParams);
        }

        /// <summary>
        /// Inserts the specified entity without triggering events.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">entity - The object type must be registered as ILiteDbSet</exception>
        public int Insert(object entity)
        {
            var set = Set(entity.GetType());

            LogSqlCommand(set.InsertDefinition, entity);

            return Connection.Query<int>(set.InsertDefinition, entity).FirstOrDefault();
        }

        /// <summary>
        /// Inserts the asynchronous without triggering events.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public async Task<int> InsertAsync(object entity)
        {
            var set = Set(entity.GetType());

            LogSqlCommand(set.InsertDefinition, entity);

            var result = await Connection.QueryAsync<int>(set.InsertDefinition, entity);

            return result.Any() ? 1 : 0;
        }

        /// <summary>
        /// Deletes the specified entity without triggering events.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The affected rows count</returns>
        public int Delete(object entity)
        {
            var set = Set(entity.GetType());

            LogSqlCommand(set.DeleteDefinition, entity);

            return Connection.Execute(set.DeleteDefinition, entity);
        }

        /// <summary>
        /// Deletes the asynchronous without triggering events.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A Task with the affected rows count</returns>
        public async Task<int> DeleteAsync(object entity)
        {
            var set = Set(entity.GetType());

            LogSqlCommand(set.DeleteDefinition, entity);

            return await Connection.ExecuteAsync(set.DeleteDefinition, entity);
        }

        /// <summary>
        /// Updates the specified entity without triggering events.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The affected rows count</returns>
        public int Update(object entity)
        {
            var set = Set(entity.GetType());

            LogSqlCommand(set.UpdateDefinition, entity);

            return Connection.Execute(set.UpdateDefinition, entity);
        }

        /// <summary>
        /// Updates the asynchronous without triggering events.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A task with he affected rows count</returns>
        public async Task<int> UpdateAsync(object entity)
        {
            var set = Set(entity.GetType());

            LogSqlCommand(set.UpdateDefinition, entity);

            return await Connection.ExecuteAsync(set.UpdateDefinition, entity);
        }

        /// <summary>
        /// Creates the database schema using the entity set DDL generators.
        /// </summary>
        private void CreateDatabase()
        {
            var ddlBuilder = new StringBuilder();
            foreach (var entitySet in _entitySets)
            {
                ddlBuilder.AppendLine(entitySet.Value.TableDefinition);
            }

            using (var tran = Connection.BeginTransaction())
            {
                Connection.Execute(ddlBuilder.ToString());
                tran.Commit();
                OnDatabaseCreated(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets all instances of Lite DB contexts that are instantiated and not disposed.
        /// </summary>
        public static ReadOnlyCollection<LiteDbContext> Instances => new ReadOnlyCollection<LiteDbContext>(Intances.Values.ToList());

        /// <summary>
        /// Gets the underlying SQLite connection.
        /// </summary>
        public IDbConnection Connection { get; private set; }
        
        /// <summary>
        /// Gets the unique identifier of this context.
        /// </summary>
        public Guid UniqueId { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enabled log].
        /// </summary>
        public bool EnabledLog { get; set; }
        
#endregion

#region IDisposable Support

        private bool _isDisposing; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        public void Dispose(bool disposing)
        {
            if (_isDisposing) return;

            if (disposing)
            {
                LiteDbContext removed;
                Intances.TryRemove(this.UniqueId, out removed);
                Connection.Close();
                Connection.Dispose();
                Connection = null;
                $"Disposed {_contextType.Name}. {Intances.Count} context instances.".Debug();
            }

            _isDisposing = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
#endregion

    }
}
