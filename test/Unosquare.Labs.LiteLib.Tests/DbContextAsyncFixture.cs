namespace Unosquare.Labs.LiteLib.Tests
{
    using NUnit.Framework;
    using System.Linq;
    using System.Threading.Tasks;
    using Database;
    using Helpers;
#if !NET452
    using Microsoft.Data.Sqlite;
#endif

    /// <summary>
    /// A TestFixture to test the included async methods in LiteDbSet
    /// </summary>
    [TestFixture]
    public class DbContextAsyncFixture
    {
        public class SelectTest : DbContextAsyncFixture
        {
            [Test]
            public async Task SelectAll()
            {
                using (var context = new TestDbContext(nameof(SelectAll)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var list = await context.Orders.SelectAllAsync();
                    Assert.AreEqual(TestHelper.DataSource.Length, list.Count(), "Same set");
                }
            }

            [Test]
            public async Task SelectWithParametersAsync()
            {
                using (var context = new TestDbContext(nameof(SelectWithParametersAsync)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    // Selecting Data By name
                    var selectingData =
                        await context.Orders.SelectAsync("CustomerName = @CustomerName", new {CustomerName = "Peter"});

                    foreach (var item in selectingData)
                    {
                        Assert.AreEqual("Peter", item.CustomerName);
                    }
                }
            }

            [Test]
            public async Task SelectDataAsync()
            {
                using (var context = new TestDbContext(nameof(SelectDataAsync)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    // Selecting Data By name
                    var selectingData =
                        await context.Orders.SelectAsync("CustomerName = @CustomerName", new {CustomerName = "Peter"});

                    foreach (var item in selectingData)
                    {
                        Assert.AreEqual("Peter", item.CustomerName);
                    }
                }
            }

            [Test]
            public async Task AsyncFirstOrDefault()
            {
                using (var context = new TestDbContext(nameof(AsyncFirstOrDefault)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var order = await context.Orders.FirstOrDefaultAsync(nameof(Order.CustomerName), "Peter");

                    Assert.IsNotNull(order);
                }
            }

            
            [Test]
            public async Task AsyncFirstOrDefaultWithLambda()
            {
                using (var context = new TestDbContext(nameof(AsyncFirstOrDefault)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var order = await context.Orders.FirstOrDefaultAsync(x => x.CustomerName, "Peter");

                    Assert.IsNotNull(order);
                }
            }
        }

        public class DeleteAsyncTest : DbContextAsyncFixture
        {
            [Test]
            public async Task DeletingAsync()
            {
                using (var context = new TestDbContext(nameof(DeletingAsync)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var incomingData = context.Orders.SelectAll();

                    foreach (var item in incomingData)
                    {
                        await context.Orders.DeleteAsync(item);
                    }

                    Assert.AreEqual(0, context.Orders.Count());
                }
            }

            [Test]
            public async Task DeletingAsyncWithParams()
            {
                using (var context = new TestDbContext(nameof(DeletingAsyncWithParams)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var deletedData =
                        await context.Orders.DeleteAsync("CustomerName = @CustomerName", new {CustomerName = "Peter"});
                    Assert.AreEqual(deletedData, TestHelper.DataSource.Count(x => x.CustomerName == "Peter"));
                }
            }
        }

        public class InsertAsyncTest : DbContextAsyncFixture
        {
            [Test]
            public async Task InsertDataAsync()
            {
                using (var context = new TestDbContext(nameof(InsertDataAsync)))
                {
                    while (context.Orders.Count() != 0)
                    {
                        var incomingData = context.Orders.SelectAll();
                        foreach (var item in incomingData)
                        {
                            await context.Orders.DeleteAsync(item);
                        }
                    }

                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var list = await context.Orders.SelectAllAsync();
                    Assert.AreEqual(TestHelper.DataSource.Length, list.Count());
                }
            }

            [Test]
            public async Task InsertAsyncUsingUnique_ThrowsSqliteException()
            {
                using (var context = new TestDbContext(nameof(InsertAsyncUsingUnique_ThrowsSqliteException)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }
#if NET452
                    Assert.ThrowsAsync<Mono.Data.Sqlite.SqliteException>(async () =>
#else
                    Assert.ThrowsAsync<SqliteException>(async () =>
#endif
                    {
                        var newOrder = new Order
                        {
                            CustomerName = "John",
                            Amount = 2,
                            ShipperCity = "Atlanta",
                            UniqueId = "1"
                        };

                        await context.Orders.InsertAsync(newOrder);
                    });
                }
            }

            [Test]
            public void InsertAsyncWithOutOfRangeString_ThrowsSqliteException()
            {
#if NET452
                Assert.ThrowsAsync<Mono.Data.Sqlite.SqliteException>(async () =>
#else
                    Assert.ThrowsAsync<SqliteException>(async () =>
#endif
                {
                    using (var context =
                        new TestDbContext(nameof(InsertAsyncWithOutOfRangeString_ThrowsSqliteException)))
                    {
                        await context.Orders.InsertAsync(new Order
                        {
                            CustomerName = "John",
                            Amount = 2,
                            ShipperCity = "StringStringStringStringStringStringStringString"
                        });
                    }
                });
            }
        }

        public class UpdateTest : DbContextAsyncFixture
        {
            [Test]
            public async Task AsyncUpdateData()
            {
                using (var context = new TestDbContext(nameof(AsyncUpdateData)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var list = await context.Orders.SelectAsync("CustomerName = @CustomerName",
                        new {CustomerName = "John"});

                    foreach (var item in list)
                    {
                        item.ShipperCity = "Atlanta";
                        await context.Orders.UpdateAsync(item);
                    }

                    var updatedList =
                        await context.Orders.SelectAsync("ShipperCity = @ShipperCity", new {ShipperCity = "Atlanta"});

                    foreach (var item in updatedList)
                    {
                        Assert.AreEqual("Atlanta", item.ShipperCity);
                    }
                }
            }
        }

        public class SingleAsyncTest : DbContextAsyncFixture
        {
            [Test]
            public async Task AsyncSelectSingleData()
            {
                using (var context = new TestDbContext(nameof(AsyncSelectSingleData)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var singleSelect = await context.Orders.SingleAsync(3);
                    Assert.AreEqual("Margarita", singleSelect.CustomerName);
                }
            }

            [Test]
            public async Task SelectingSingleDataWithIncorrectId_ReturnsNull()
            {
                using (var context = new TestDbContext(nameof(SelectingSingleDataWithIncorrectId_ReturnsNull)))
                {
                    var k = 0;

                    foreach (var item in TestHelper.DataSource)
                    {
                        item.UniqueId = (k++).ToString();
                        await context.Orders.InsertAsync(item);
                    }

                    var singleSelect = await context.Orders.SingleAsync(50);
                    Assert.IsNull(singleSelect);
                }
            }
        }

        public class SetTestAsync : DbContextAsyncFixture
        {
            [Test]
            public async Task AsyncInsertFromSetName()
            {
                using (var context = new TestDbContext(nameof(AsyncInsertFromSetName)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.InsertAsync(item);
                    }

                    Assert.AreEqual(TestHelper.DataSource.Length, context.Orders.Count(), "Has data");
                }
            }

            [Test]
            public async Task AsyncDeleteFromSetName()
            {
                using (var context = new TestDbContext(nameof(AsyncDeleteFromSetName)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.InsertAsync(item);
                    }

                    Assert.AreEqual(TestHelper.DataSource.Length, context.Orders.Count(), "Has data");

                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.DeleteAsync(item);
                    }

                    Assert.AreEqual(0, context.Orders.Count(), "Has data");
                }
            }

            [Test]
            public async Task AsyncUpdateFromSetName()
            {
                using (var context = new TestDbContext(nameof(AsyncUpdateFromSetName)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.InsertAsync(item);
                    }

                    foreach (var item in TestHelper.DataSource)
                    {
                        item.ShipperCity = "Atlanta";
                        await context.UpdateAsync(item);
                    }

                    var updatedItems =
                        await context.Orders.SelectAsync("ShipperCity = @ShipperCity", new {ShipperCity = "Atlanta"});
                    Assert.AreEqual(TestHelper.DataSource.Length, updatedItems.Count());
                }
            }
        }

        public class QueryAsync : DbContextAsyncFixture
        {
            [Test]
            public async Task AsyncQueryData()
            {
                using (var context = new TestDbContext(nameof(AsyncQueryData)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var selectedData =
                        await
                            context.QueryAsync<Order>(
                                $"{context.Orders.SelectDefinition} WHERE CustomerName = @CustomerName",
                                new Order {CustomerName = "John"});

                    foreach (var item in selectedData)
                    {
                        Assert.AreEqual(item.CustomerName, "John");
                    }
                }
            }
        }

        public class CountAsync : DbContextAsyncFixture
        {
            [Test]
            public async Task AsyncCountData()
            {
                using (var context = new TestDbContext(nameof(AsyncCountData)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var selectingData =
                        await context.Orders.CountAsync();

                    Assert.AreEqual(12, selectingData);
                }
            }

            [Test]
            public async Task CountingDataWithParams()
            {
                using (var context = new TestDbContext(nameof(CountingDataWithParams)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var selectingData =
                        await context.Orders.CountAsync("CustomerName = @CustomerName", new {CustomerName = "John"});

                    Assert.AreEqual(4, selectingData);
                }
            }
        }

        public class AnyAsyncTest : DbContextAsyncFixture
        {
            [Test]
            public async Task AnyAsyncMethod_ShouldPass()
            {
                using (var context = new TestDbContext(nameof(AnyAsyncMethod_ShouldPass)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var result = await context.Orders.AnyAsync();

                    Assert.IsTrue(result);
                }
            }

            [Test]
            public async Task AnyAsyncMethodWithParams_ShouldPass()
            {
                using (var context = new TestDbContext(nameof(AnyAsyncMethodWithParams_ShouldPass)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        await context.Orders.InsertAsync(item);
                    }

                    var result =
                        await context.Orders.AnyAsync("CustomerName = @CustomerName", new {CustomerName = "John"});

                    Assert.IsTrue(result);
                }
            }

            [Test]
            public async Task AnyAsyncMethodWithParams_ShouldFail()
            {
                using (var context = new TestDbContext(nameof(AnyAsyncMethodWithParams_ShouldFail)))
                {
                    var result = await context.Orders.AnyAsync("CustomerName", "Fail");

                    Assert.IsFalse(result);
                }
            }
        }
    }
}