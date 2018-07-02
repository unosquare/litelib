namespace Unosquare.Labs.LiteLib.Tests.Database
{
    internal class Warehouse : LiteModel
    {
        [LiteUnique]
        public string UniqueId { get; set; }

        public string Name { get; set; }
    }
}