using Unosquare.Labs.LiteLib.Log;
using Unosquare.Labs.LiteLib.Tests.Helpers;

namespace Unosquare.Labs.LiteLib.Tests.Database
{
    class TestDbContext : LiteDbContext
    {
        public TestDbContext(string name) : base(TestHelper.GetTempDb(name ?? "empty"), new NullLog())
        {
        }

        public LiteDbSet<Order> Orders { get; set; }
    }
}
