using Unosquare.Labs.LiteLib.Tests.Helpers;

namespace Unosquare.Labs.LiteLib.Tests.Database
{
    internal class TestDbContext : LiteDbContext
    {
        public TestDbContext(string name) : base(TestHelper.GetTempDb(name ?? "empty"))
        {
        }

        public LiteDbSet<Order> Orders { get; set; }

        public LiteDbSet<Warehouse> Warehouses { get; set; }
    }
}