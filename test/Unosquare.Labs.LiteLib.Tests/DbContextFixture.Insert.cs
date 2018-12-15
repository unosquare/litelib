namespace Unosquare.Labs.LiteLib.Tests
{
    using Database;
    using Helpers;
    using NUnit.Framework;
    using System.Linq;
#if !NET452
    using Microsoft.Data.Sqlite;
#endif

    public partial class DbContextFixture
    {
        public class Insert : DbContextFixture
        {
            [Test]
            public void InsertData()
            {
                using (var context = new TestDbContext(nameof(InsertData)))
                {
                    var entity = TestHelper.DataSource.First();
                    context.Orders.Insert(entity);

                    Assert.AreNotEqual(0, entity.RowId, "Has a RowId value");
                }
            }

            [Test]
            public void InsertUsingUnique_ThrowsSqliteException()
            {
                using (var context = new TestDbContext(nameof(InsertUsingUnique_ThrowsSqliteException)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

#if NET452
                    Assert.Throws<Mono.Data.Sqlite.SqliteException>(() =>
#else
                    Assert.Throws<SqliteException>(() =>
#endif
                    {
                        var newOrder = new Order
                        {
                            CustomerName = "John",
                            Amount = 2,
                            ShipperCity = "Atlanta",
                            UniqueId = "1"
                        };
                        context.Orders.Insert(newOrder);
                    });
                }
            }

            [Test]
            public void InsertingWithOutOfRangeString_ThrowsSqliteException()
            {
                using (var context = new TestDbContext(nameof(InsertingWithOutOfRangeString_ThrowsSqliteException)))
                {
#if NET452
                    Assert.Throws<Mono.Data.Sqlite.SqliteException>(() =>
#else
                    Assert.Throws<SqliteException>(() =>
#endif
                    {
                        context.Orders.Insert(new Order
                        {
                            CustomerName = "John",
                            Amount = 2,
                            ShipperCity = "StringStringStringStringStringStringStringString"
                        });
                    });
                }
            }
        }
    }
}