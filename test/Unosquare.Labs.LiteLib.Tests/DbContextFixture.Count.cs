namespace Unosquare.Labs.LiteLib.Tests
{
    using Database;
    using Helpers;
    using NUnit.Framework;
#if !NET461
#endif

    public partial class DbContextFixture
    {
        public class Count : DbContextFixture
        {
            [Test]
            public void CountingData()
            {
                using (var context = new TestDbContext(nameof(CountingData)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    Assert.AreEqual(12, context.Orders.Count());
                }
            }

            [Test]
            public void CountingDataWithParams()
            {
                using (var context = new TestDbContext(nameof(CountingDataWithParams)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    Assert.AreEqual(4,
                        context.Orders.Count("CustomerName = @CustomerName", new {CustomerName = "John"}));
                }
            }
        }
    }
}