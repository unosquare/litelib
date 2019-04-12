namespace Unosquare.Labs.LiteLib.Tests
{
    using Database;
    using Helpers;
    using NUnit.Framework;
#if !NET461
    using Microsoft.Data.Sqlite;
#endif

    public partial class DbContextFixture
    {
        public class Query : DbContextFixture
        {
            [Test]
            public void SelectingData()
            {
                using (var context = new TestDbContext(nameof(SelectingData)))
                {
                    var k = 0;
                    for (var i = 0; i < 10; i++)
                    {
                        foreach (var item in TestHelper.DataSource)
                        {
                            item.UniqueId = (k++).ToString();
                            context.Orders.Insert(item);
                        }
                    }

                    var selectedData =
                        context.Query<Order>($"{context.Orders.SelectDefinition} WHERE CustomerName = @CustomerName",
                            new Order {CustomerName = "Margarita"});

                    foreach (var item in selectedData)
                    {
                        Assert.AreEqual(item.CustomerName, "Margarita");
                    }
                }
            }

            [Test]
            public void UsingBadQuery_ThrowsSqliteException()
            {
                using (var context = new TestDbContext(nameof(UsingBadQuery_ThrowsSqliteException)))
                {
                    var k = 0;
                    for (var i = 0; i < 10; i++)
                    {
                        foreach (var item in TestHelper.DataSource)
                        {
                            item.UniqueId = (k++).ToString();
                            context.Orders.Insert(item);
                        }
                    }

#if NET461
                    Assert.Throws<Mono.Data.Sqlite.SqliteException>(() =>
#else
                    Assert.Throws<SqliteException>(() =>
#endif
                    {
                        context.Query<Order>(
                            $"{context.Orders.UpdateDefinition} WHERE CustomerName = @CustomerName",
                            new Order {CustomerName = "Margarita"});
                    });
                }
            }
        }
    }
}