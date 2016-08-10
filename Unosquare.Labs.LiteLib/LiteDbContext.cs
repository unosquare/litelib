namespace Unosquare.Labs.LiteLib
{
    using Dapper;
    using Log;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.SQLite;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A base class containing all the functionality to perform data operations on Entity Sets
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public abstract class LiteDbContext : IDisposable
    {

        #region Private Declarations 

        private SQLiteConnection m_Connection;
        private readonly Dictionary<string, ILiteDbSet> EntitySets = new Dictionary<string, ILiteDbSet>();
        private readonly Type ContextType = null;
        static private readonly ConcurrentDictionary<Guid, LiteDbContext> m_Intances = new ConcurrentDictionary<Guid, LiteDbContext>();
        static private readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyInfoCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        static private readonly Type GenericLiteDbSetType = typeof(LiteDbSet<>);

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
        /// <param name="logger">The logger.</param>
        protected LiteDbContext(string databaseFilePath, ILog logger)
        {
            Logger = logger ?? new NullLog();
            ContextType = GetType();
            LoadEntitySets();

            databaseFilePath = Path.GetFullPath(databaseFilePath);
            var databaseExists = File.Exists(databaseFilePath);
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = databaseFilePath,
                DateTimeKind = DateTimeKind.Utc
            };

            m_Connection = new SQLiteConnection(builder.ToString());
            m_Connection.Open();

            if (databaseExists == false)
            {
                Logger.DebugFormat("DB file does not exist. Creating.");
                CreateDatabase();
                Logger.DebugFormat($"DB file created: '{databaseFilePath}'");
            }

            UniqueId = Guid.NewGuid();
            m_Intances[UniqueId] = this;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the entity sets registered as vitual public properties of the derived class.
        /// </summary>
        private void LoadEntitySets()
        {
            PropertyInfo[] contextDbSetProperties;
            if (PropertyInfoCache.ContainsKey(ContextType))
            {
                contextDbSetProperties = PropertyInfoCache[ContextType];
            }
            else
            {
                contextDbSetProperties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == GenericLiteDbSetType).ToArray();
                PropertyInfoCache[ContextType] = contextDbSetProperties;
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
                EntitySets[entitySetProp.Name] = currentValue;
            }

            Logger.DebugFormat($"Context intance {ContextType.Name} - {EntitySets.Count} entity sets. {Instances.Count} context intances.");
        }

        /// <summary>
        /// Creates the database schema using the entity set DDL generators.
        /// </summary>
        private void CreateDatabase()
        {
            var ddlBuilder = new StringBuilder();
            foreach (var entitySet in EntitySets)
            {
                ddlBuilder.AppendLine(entitySet.Value.TableDefinition);
            }

            using (var tran = m_Connection.BeginTransaction())
            {
                m_Connection.Execute(ddlBuilder.ToString());
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
            Logger.DebugFormat("DB VACUUM command executing.");
            await Connection.ExecuteAsync("VACCUUM");
            Logger.DebugFormat("DB VACUUM command finished.");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the underlying SQLite connection.
        /// </summary>
        public SQLiteConnection Connection => m_Connection;

        /// <summary>
        /// Gets the logger this instance was initialized with.
        /// </summary>
        public ILog Logger { get; protected set; }

        /// <summary>
        /// Gets the unique identifier of this context.
        /// </summary>
        public Guid UniqueId { get; protected set; }

        /// <summary>
        /// Gets all instances of Lite DB contexts that are instantiated and not disposed.
        /// </summary>
        public static ReadOnlyCollection<LiteDbContext> Instances => new ReadOnlyCollection<LiteDbContext>(m_Intances.Values.ToList());

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
                    LiteDbContext removed = null;
                    m_Intances.TryRemove(this.UniqueId, out removed);
                    m_Connection.Close();
                    m_Connection.Dispose();
                    m_Connection = null;
                    Logger.DebugFormat($"Disposed {ContextType.Name}. {m_Intances.Count} context instances.");
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
