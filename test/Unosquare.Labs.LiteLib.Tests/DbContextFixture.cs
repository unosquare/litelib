using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unosquare.Labs.LiteLib.Tests.Database;
using Unosquare.Labs.LiteLib.Tests.Helpers;

#if MONO
using Mono.Data.Sqlite;
#elif NET46

using System.Data.SQLite;

#else
using Microsoft.Data.Sqlite;
#endif

namespace Unosquare.Labs.LiteLib.Tests
{
    /// <summary>
    /// A TestFixture to test the included methods in LiteDbSet
    /// </summary>
    [TestFixture]
    public class DbContextFixture
    {
        public class SelectTest : DbContextFixture
        {
            /// <summary>
            /// Tests the select all.
            /// </summary>
            [Test]
            public void SelectAllData()
            {
                using (var context = new TestDbContext(nameof(SelectAllData)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var list = context.Orders.SelectAll();
                    Assert.AreEqual(TestHelper.DataSource.Length, list.Count(), "Same set");
                }
            }

            [Test]
            public void SelectDataWithParameters()
            {
                using (var context = new TestDbContext(nameof(SelectDataWithParameters)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var entities = context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "John" });
                    foreach (var item in entities)
                    {
                        Assert.AreEqual("John", item.CustomerName);
                    }
                }
            }

            [Test]
            public void SelectWithBadParameters_ThrowsException()
            {
                using (var context = new TestDbContext(nameof(SelectWithBadParameters_ThrowsException)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }
#if NET46
                    Assert.Throws<SQLiteException>(() =>
#else
                    Assert.Throws<SqliteException>(() =>
#endif
                    {
                        var entities = context.Orders.Select("Customer = @CustomerName", new { CustomerName = "John" });
                    });
                }
            }

            [Test]
            public void SelectingFirstOrDefault()
            {
                using (var context = new TestDbContext(nameof(SelectingFirstOrDefault)))
                {
                    var id = context.Orders.Insert(TestHelper.DataSource.First());
                    Assert.AreNotEqual(0, id);

                    var order = context.Orders.FirstOrDefault(nameof(Order.CustomerName), "John");

                    Assert.IsNotNull(order);
                }
            }
        }

        public class DeleteTest : DbContextFixture
        {
            [Test]
            public void DeletingData()
            {
                using (var context = new TestDbContext(nameof(DeletingData)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var incomingData = context.Orders.SelectAll();
                    foreach (var item in incomingData)
                    {
                        context.Orders.Delete(item);
                    }
                    Assert.AreEqual(0, context.Orders.Count());
                }
            }

            [Test]
            public void DeletingUsingParams()
            {
                using (var context = new TestDbContext(nameof(DeletingUsingParams)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var deletedData =
                        context.Orders.Delete("CustomerName = @CustomerName", new { CustomerName = "Peter" });
                    Assert.AreEqual(deletedData, TestHelper.DataSource.Count(x => x.CustomerName == "Peter"));
                }
            }

            [Test]
            public void DeletingUsingInvalidParams_TrhowsSqliteException()
            {
                using (var context = new TestDbContext(nameof(DeletingUsingParams)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

#if NET46
                    Assert.Throws<SQLiteException>(() =>
#else
                    Assert.Throws<SqliteException>(() =>
#endif
                    {
                        var deletedData =
                            context.Orders.Delete("Customer = @CustomerName", new { CustomerName = "Peter" });
                    });
                }
            }
        }

        public class InsertTest : DbContextFixture
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

#if NET46
                    Assert.Throws<SQLiteException>(() =>
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
#if NET46
                    Assert.Throws<SQLiteException>(() =>
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

        public class InsertRangeTest : DbContextFixture
        {
            [Test]
            public void InsertingDataList()
            {
                using (var context = new TestDbContext(nameof(InsertingDataList)))
                {
                    context.Orders.InsertRange(TestHelper.DataSource);
                    var list = context.Orders.Count();
                    Assert.AreEqual(TestHelper.DataSource.Length, list);
                }
            }

            [Test]
            public void InsertingEmptyDataList_TrhowsArgumentException()
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    using (var context = new TestDbContext(nameof(InsertingEmptyDataList_TrhowsArgumentException)))
                    {
                        context.Orders.InsertRange(new List<Order>());
                    }
                });
            }
        }

        public class UpdateTest : DbContextFixture
        {
            [Test]
            public void UpdatingEntities()
            {
                using (var context = new TestDbContext(nameof(UpdatingEntities)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var list = context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "John" });
                    foreach (var item in list)
                    {
                        item.ShipperCity = "Atlanta";
                        context.Orders.Update(item);
                    }

                    var updatedList =
                        context.Orders.Select("ShipperCity = @ShipperCity", new { ShipperCity = "Atlanta" });
                    foreach (var item in updatedList)
                    {
                        Assert.AreEqual("Atlanta", item.ShipperCity);
                    }
                }
            }
        }

        public class SinigleTest : DbContextFixture
        {
            [Test]
            public void SelectingSingleDataWithCorrectId()
            {
                using (var cotext = new TestDbContext(nameof(SelectingSingleDataWithCorrectId)))
                {
                    var k = 0;
                    foreach (var item in TestHelper.DataSource)
                    {
                        item.UniqueId = (k++).ToString();
                        cotext.Orders.Insert(item);
                    }

                    var singleSelect = cotext.Orders.Single(3);
                    Assert.AreEqual("Margarita", singleSelect.CustomerName);
                }
            }

            [Test]
            public void SelectingSingleDataWithIncorrectId_ReturnsNull()
            {
                using (var cotext = new TestDbContext(nameof(SelectingSingleDataWithIncorrectId_ReturnsNull)))
                {
                    var k = 0;

                    foreach (var item in TestHelper.DataSource)
                    {
                        item.UniqueId = (k++).ToString();
                        cotext.Orders.Insert(item);
                    }

                    var singleSelect = cotext.Orders.Single(50);
                    Assert.IsNull(singleSelect);
                }
            }
        }

        public class SetTest : DbContextFixture
        {
            [Test]
            public void SetEntity()
            {
                using (var context = new TestDbContext(nameof(SetEntity)))
                {
                    var names = context.GetSetNames();
                    Assert.IsNotNull(names);
                    Assert.AreEqual(names, new[] { nameof(context.Orders), nameof(context.Warehouses) });

                    var orders = context.Set<Order>();
                    var ordersByName = context.Set(typeof(Order));

                    Assert.AreEqual(context.Orders, orders);
                    Assert.AreEqual(context.Orders, ordersByName);
                }
            }

            [Test]
            public void SelectFromSetname()
            {
                using (var context = new TestDbContext(nameof(SelectFromSetname)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var orders = context.Set<Order>();

                    var data = context.Select<Order>(orders, "1=1");
                    Assert.IsNotNull(data);
                    Assert.AreEqual(context.Orders.SelectAll().First().RowId, data.First().RowId, "Same first object");
                }
            }

            [Test]
            public void InvalidSetname_ThrowsArgumentOutOfRangeException()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    using (var context = new TestDbContext(nameof(InvalidSetname_ThrowsArgumentOutOfRangeException)))
                    {
                        context.Set<DbContextFixture>();
                    }
                });
            }

            [Test]
            public void InsertFromSetname()
            {
                using (var context = new TestDbContext(nameof(InsertFromSetname)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Insert(item);
                    }

                    Assert.AreEqual(TestHelper.DataSource.Length, context.Orders.Count(), "Has data");
                }
            }

            [Test]
            public void DeleteFromSetname()
            {
                using (var context = new TestDbContext(nameof(DeleteFromSetname)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Insert(item);
                    }

                    Assert.AreEqual(TestHelper.DataSource.Length, context.Orders.Count(), "Has data");

                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Delete(item);
                    }

                    Assert.AreEqual(0, context.Orders.Count(), "Has data");
                }
            }

            [Test]
            public void UpdateFromSetname()
            {
                using (var context = new TestDbContext(nameof(UpdateFromSetname)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Insert(item);
                    }

                    foreach (var item in TestHelper.DataSource)
                    {
                        item.ShipperCity = "Atlanta";
                        context.Update(item);
                    }
                    var updatedItems =
                        context.Orders.Select("ShipperCity = @ShipperCity", new { ShipperCity = "Atlanta" });
                    Assert.AreEqual(TestHelper.DataSource.Length, updatedItems.Count());
                }
            }
        }

        public class Qerytest : DbContextFixture
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
                            new Order { CustomerName = "Margarita" });

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

#if NET46
                    Assert.Throws<SQLiteException>(() =>
#else
                    Assert.Throws<SqliteException>(() =>
#endif
                    {
                        var selectedData =
                            context.Query<Order>(
                                $"{context.Orders.UpdateDefinition} WHERE CustomerName = @CustomerName",
                                new Order { CustomerName = "Margarita" });
                    });
                }
            }
        }

        public class CountAsync : DbContextFixture
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

                    var selectingData =
                        context.Orders.Count();

                    Assert.AreEqual(12, selectingData);
                }
            }
        }
    }
}