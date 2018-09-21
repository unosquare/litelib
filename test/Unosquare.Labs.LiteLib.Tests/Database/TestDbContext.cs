namespace Unosquare.Labs.LiteLib.Tests.Database
{
    using Helpers;

    internal class TestDbContext : LiteDbContext
    {
        public TestDbContext(string name) : base(TestHelper.GetTempDb(name ?? "empty"))
        {
        }

        public LiteDbSet<Order> Orders { get; set; }

        public LiteDbSet<Warehouse> Warehouses { get; set; }
    }

    internal class TestDbContextWithOutProperties : LiteDbContext
    {
        public TestDbContextWithOutProperties(string name) : base(TestHelper.GetTempDb(name ?? "empty"))
        {
        }

        public LiteDbSet<Order> Orders { get; set; }

        public LiteDbSet<Warehouse> Warehouses { get; set; }

        public LiteDbSet<Extra> Extras { set; get; }
    }
}