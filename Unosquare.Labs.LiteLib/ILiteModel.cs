namespace Unosquare.Labs.LiteLib
{
    using System;

    /// <summary>
    /// Provides minimum contract on which a class can be considered a model for
    /// a SQLite entity set.
    /// </summary>
    public interface ILiteModel
    {
        /// <summary>
        /// Gets or sets the native SQLite row identifier.
        /// </summary>
        long RowId { get; set; }
    }

    /// <summary>
    /// Base model class for ISQLiteEntity.
    /// Inherit from this model if you don't want to implement the RowId property.
    /// </summary>
    public abstract class LiteModel : ILiteModel
    {
        /// <summary>
        /// Gets or sets the native SQLite row identifier.
        /// </summary>
        public long RowId { get; set; }
    }

    /// <summary>
    /// Hints the DDL generator that an index needs to be created.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    public class LiteIndexAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LiteIndexAttribute"/> class.
        /// </summary>
        public LiteIndexAttribute() { }
    }

    /// <summary>
    /// Hints the DDL generator that a UNIQUE index needs to be created.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    public class LiteUniqueAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LiteIndexAttribute" /> class.
        /// </summary>
        public LiteUniqueAttribute() { }
    }
}
