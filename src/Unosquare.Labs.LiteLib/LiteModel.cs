namespace Unosquare.Labs.LiteLib
{
    /// <summary>
    /// Base model class for ISQLiteEntity.
    /// Inherit from this model if you don't want to implement the RowId property.
    /// </summary>
    public abstract class LiteModel : ILiteModel
    {
        /// <inheritdoc />
        public long RowId { get; set; }
    }
}