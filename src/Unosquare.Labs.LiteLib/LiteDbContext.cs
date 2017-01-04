namespace Unosquare.Labs.LiteLib
{
    using Dapper;
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
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyInfoCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly Type GenericLiteDbSetType = typeof(LiteDbSet<>);

#endregion

#region Events

        /// <summary>
        /// Occurs when [on database created].
        /// </summary>
        public event EventHandler OnDatabaseCreated = (s, e) => { };

#endregion

#region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbContext" /> class.
        /// </summary>
        /// <param name="databaseFilePath">The database file path.</param>
        protected LiteDbContext(string databaseFilePath)
        {
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
                //DateTimeKind = DateTimeKind.Utc
            };

            Connection = new SqliteConnection(builder.ToString());
#endif
            Connection.Open();

            if (databaseExists == false)
            {
                "DB file does not exist. Creating.".Debug();
                CreateDatabase();
                $"DB file created: '{databaseFilePath}'".Debug();
            }

            UniqueId = Guid.NewGuid();
            Intances[UniqueId] = this;
        }

#endregion

#region Methods

        /// <summary>
        /// Loads the entity sets registered as virtual public properties of the derived class.
        /// </summary>
        private void LoadEntitySets()
        {
            PropertyInfo[] contextDbSetProperties;
            if (PropertyInfoCache.ContainsKey(_contextType))
            {
                contextDbSetProperties = PropertyInfoCache[_contextType];
            }
            else
            {
                contextDbSetProperties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType.GetTypeInfo().IsGenericType && p.PropertyType.GetGenericTypeDefinition() == GenericLiteDbSetType).ToArray();
                PropertyInfoCache[_contextType] = contextDbSetProperties;
            }
            
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

            $"Context instance {_contextType.Name} - {_entitySets.Count} entity sets. {Instances.Count} context instances.".Debug();
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

        /// <summary>
        /// Vacuums the database asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task VaccuumDatabaseAsync()
        {
            "DB VACUUM command executing.".Debug();
            await Connection.ExecuteAsync("VACCUUM");
            "DB VACUUM command finished.".Debug();
        }

#endregion

#region Properties

        /// <summary>
        /// Gets the underlying SQLite connection.
        /// </summary>
        public IDbConnection Connection { get; private set; }
        
        /// <summary>
        /// Gets the unique identifier of this context.
        /// </summary>
        public Guid UniqueId { get; protected set; }

        /// <summary>
        /// Gets all instances of Lite DB contexts that are instantiated and not disposed.
        /// </summary>
        public static ReadOnlyCollection<LiteDbContext> Instances => new ReadOnlyCollection<LiteDbContext>(Intances.Values.ToList());

#endregion

#region IDisposable Support

        private bool _isDisposing; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        void Dispose(bool disposing)
        {
            if (!_isDisposing)
            {
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
