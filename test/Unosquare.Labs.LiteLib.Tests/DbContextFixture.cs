using NUnit.Framework;
using System;
using System.Collections.Generic;
#if MONO
    using Mono.Data.Sqlite;
#else
using Microsoft.Data.Sqlite;
#endif
using System.Linq;
using System.Threading.Tasks;
using Unosquare.Labs.LiteLib.Tests.Database;

namespace Unosquare.Labs.LiteLib.Tests
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (var context = new TestDbContext(nameof(Program)))
            {
                while (context.Orders.Count() != 0)
                {
                    var incomingData = context.Orders.SelectAll();
                    foreach (var item in incomingData)
                    {
                        context.Orders.Delete(item);
                    }
                }

                context.Orders.InsertRange(DbContextFixture.DataSource);
                var list = context.Orders.SelectAll();
                Assert.AreEqual(DbContextFixture.DataSource.Length, list.Count());
            }
        }
    }

    /// <summary>
    /// A TestFixture to test the included methods in LiteDbSet
    /// </summary>
    [TestFixture]
    public class DbContextFixture
    {
        /// <summary>
        /// The data source for all Test
        /// </summary>
        internal static readonly Order[] DataSource =
        {
            new Order { UniqueId = "1", CustomerName = "John", ShipperCity = "Guadalajara", Amount = 4, IsShipped = true, ShippedDate = DateTime.UtcNow },
            new Order { UniqueId = "2", CustomerName = "Peter", ShipperCity = "Leon", Amount = 6},
            new Order { UniqueId = "3", CustomerName = "Margarita", ShipperCity = "Boston", Amount = 7, IsShipped = true, ShippedDate = DateTime.UtcNow },
            new Order { UniqueId = "4", CustomerName = "John", ShipperCity = "Guadalajara", Amount = 4},
            new Order { UniqueId = "5", CustomerName = "Peter", ShipperCity = "Leon", Amount = 6},
            new Order { UniqueId = "6", CustomerName = "Margarita", ShipperCity = "Boston", Amount = 7},
            new Order { UniqueId = "7", CustomerName = "John", ShipperCity = "Guadalajara", Amount = 4},
            new Order { UniqueId = "8", CustomerName = "Peter", ShipperCity = "Leon", Amount = 6},
            new Order { UniqueId = "9", CustomerName = "Margarita", ShipperCity = "Boston", Amount = 7},
            new Order { UniqueId = "10", CustomerName = "John", ShipperCity = "Guadalajara", Amount = 4},
            new Order { UniqueId = "11", CustomerName = "Peter", ShipperCity = "Leon", Amount = 6},
            new Order { UniqueId = "12", CustomerName = "Margarita", ShipperCity = "Boston", Amount = 7}
        };

        /// <summary>
        /// Tests the select all.
        /// </summary>
        [Test]
        public void TestSelectAll()
        {
            using (var context = new TestDbContext(nameof(TestSelectAll)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                var list = context.Orders.SelectAll();
                Assert.AreEqual(DataSource.Length, list.Count(), "Same set");
            }
        }

        /// <summary>
        /// Tests the delete data.
        /// </summary>
        [Test]
        public void TestDeleteData()
        {
            using (var context = new TestDbContext(nameof(TestDeleteData)))
            {
                foreach (var item in DataSource)
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

        /// <summary>
        /// Tests the insert data.
        /// </summary>
        [Test]
        public void TestInsertData()
        {
            using (var context = new TestDbContext(nameof(TestInsertData)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                var list = context.Orders.SelectAll();
                Assert.AreEqual(DataSource.Length, list.Count());
            }
        }


        /// <summary>
        /// Tests the update data.
        /// </summary>
        [Test]
        public void TestUpdateData()
        {
            using (var context = new TestDbContext(nameof(TestUpdateData)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                var list = context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "John" });
                foreach (var item in list)
                {
                    item.ShipperCity = "Atlanta";
                    context.Orders.Update(item);
                }

                var updatedList = context.Orders.Select("ShipperCity = @ShipperCity", new { ShipperCity = "Atlanta" });
                foreach (var item in updatedList)
                {
                    Assert.AreEqual("Atlanta", item.ShipperCity);
                }
            }
        }


        /// <summary>
        /// Tests the select data.
        /// </summary>
        [Test]
        public void TestSelectData()
        {
            using (var context = new TestDbContext(nameof(TestSelectData)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                var selectingData = context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Peter" });
                foreach (var item in selectingData)
                {
                    Assert.AreEqual("Peter", item.CustomerName);
                }
            }
        }


        /// <summary>
        /// Tests the count data.
        /// </summary>
        [Test]
        public void TestCountData()
        {
            using (var context = new TestDbContext(nameof(TestCountData)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                var selectingData = context.Orders.Select("CustomerName = @CustomerName",
                    new { CustomerName = "Margarita" });
                Assert.AreEqual(4, selectingData.Count());
            }
        }

        /// <summary>
        /// Tests the single data.
        /// </summary>
        [Test]
        public void TestSingleData()
        {
            using (var cotext = new TestDbContext(nameof(TestSingleData)))
            {
                var k = 0;
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in DataSource)
                    {
                        item.UniqueId = (k++).ToString();
                        cotext.Orders.Insert(item);
                    }
                }

                var singleSelect = cotext.Orders.Single(3);
                Assert.AreEqual("Margarita", singleSelect.CustomerName);
            }
        }

        /// <summary>
        /// Tests the query data.
        /// </summary>
        [Test]
        public void TestQueryData()
        {
            using (var context = new TestDbContext(nameof(TestQueryData)))
            {
                var k = 0;
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in DataSource)
                    {
                        item.UniqueId = (k++).ToString();
                        context.Orders.Insert(item);
                    }
                }

                var selectedData =
                    context.Orders.Query($"{context.Orders.SelectDefinition} WHERE CustomerName = @CustomerName",
                        new Order { CustomerName = "Margarita" });
                foreach (var item in selectedData)
                {
                    Assert.IsTrue(item.CustomerName == "Margarita");
                }
            }
        }

        /// <summary>
        /// Called when [before insert test].
        /// </summary>
        [Test]
        public void OnBeforeInsertTest()
        {
            using (var context = new TestDbContext(nameof(OnBeforeInsertTest)))
            {
                context.Orders.OnBeforeInsert += (s, e) =>
                {
                    if (e.Entity.CustomerName == "Peter")
                    {
                        e.Entity.CustomerName = "Charles";
                    }
                };

                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                var updatedList = context.Orders.Select("ShipperCity = @ShipperCity", new { ShipperCity = "Leon" });
                foreach (var item in updatedList)
                {
                    Assert.AreNotEqual("Peter", item.CustomerName);
                }
            }
        }

        /// <summary>
        /// Called when [after insert].
        /// </summary>
        [Test]
        public void OnAfterInsert()
        {
            var context = new TestDbContext(nameof(OnAfterInsert));

            context.Orders.OnAfterInsert += (s, e) =>
            {
                if (e.Entity.CustomerName == "John" || e.Entity.CustomerName == "Peter")
                {
                    context.Orders.Delete(e.Entity);
                }
            };

            foreach (var item in DataSource)
            {
                context.Orders.Insert(item);
            }

            var afterList = context.Orders.SelectAll().ToList();

            foreach (var item in afterList)
            {
                Assert.AreEqual("Margarita", item.CustomerName);
            }

            Assert.AreEqual(4, afterList.Count());
        }

        /// <summary>
        /// Called when [before update test].
        /// </summary>
        [Test]
        public void OnBeforeUpdateTest()
        {
            using (var context = new TestDbContext(nameof(OnBeforeUpdateTest)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                context.Orders.OnBeforeUpdate += (s, e) =>
                {
                    if (e.Entity.ShipperCity == "Leon")
                    {
                        e.Entity.ShipperCity = "Atlanta";
                    }
                };

                foreach (var item in context.Orders.SelectAll())
                {
                    context.Orders.Update(item);
                }

                var updatedList = context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Peter" });
                foreach (var item in updatedList)
                {
                    Assert.AreEqual("Atlanta", item.ShipperCity);
                }
            }
        }

        /// <summary>
        /// Called when [after update test].
        /// </summary>
        [Test]
        public void OnAfterUpdateTest()
        {
            using (var context = new TestDbContext(nameof(OnAfterUpdateTest)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                var newDataSource = new List<Order>();
                context.Orders.OnAfterUpdate += (s, e) =>
                {
                    if (e.Entity.ShipperCity == "Guadalajara")
                    {
                        newDataSource.Add(e.Entity);
                    }
                };

                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in context.Orders.SelectAll())
                    {
                        context.Orders.Update(item);
                    }
                }

                Assert.AreEqual(40, newDataSource.Count());
            }
        }

        /// <summary>
        /// Called when [before delete test].
        /// </summary>
        [Test]
        public void OnBeforeDeleteTest()
        {
            using (var context = new TestDbContext(nameof(OnBeforeDeleteTest)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                var deletedList = new List<Order>();
                context.Orders.OnBeforeDelete += (s, e) =>
                {
                    deletedList.Add(e.Entity);
                };

                foreach (var item in context.Orders.SelectAll())
                {
                    if (item.CustomerName == "John")
                    {
                        context.Orders.Delete(item);
                    }
                }

                Assert.AreEqual(4, deletedList.Count());
            }
        }

        //Test OnAfterDelete
        [Test]
        public void OnAfterDeleteTest()
        {
            var context = new TestDbContext(nameof(OnAfterDeleteTest));
            foreach (var item in DataSource)
            {
                context.Orders.Insert(item);
            }

            context.Orders.OnAfterDelete += (s, e) =>
            {
                e.Entity.CustomerName = "Jessy";
                context.Orders.Insert(e.Entity);
            };

            foreach (var item in context.Orders.SelectAll())
            {
                if (item.CustomerName == "Margarita")
                {
                    context.Orders.Delete(item);
                }
            }

            foreach (var item in context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Jessy" })
            )
            {
                Assert.AreEqual("Jessy", item.CustomerName);
            }
        }

        /// <summary>
        /// Asynchronous the test select all.
        /// </summary>
        [Test]
        public async Task AsyncTestSelectAll()
        {
            using (var context = new TestDbContext(nameof(AsyncTestSelectAll)))
            {
                foreach (var item in DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }

                var list = await context.Orders.SelectAllAsync();
                Assert.AreEqual(DataSource.Count(), list.Count(), "Same set");
            }
        }

        /// <summary>
        /// Asynchronous the test delete data.
        /// </summary>
        [Test]
        public async Task AsyncTestDeleteData()
        {
            using (var context = new TestDbContext(nameof(AsyncTestDeleteData)))
            {
                foreach (var item in DataSource)
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

        /// <summary>
        /// Asynchronous the test insert data.
        /// </summary>
        [Test]
        public async Task AsyncTestInsertData()
        {
            using (var context = new TestDbContext(nameof(AsyncTestInsertData)))
            {
                while (context.Orders.Count() != 0)
                {
                    var incomingData = context.Orders.SelectAll();
                    foreach (var item in incomingData)
                    {
                        await context.Orders.DeleteAsync(item);
                    }
                }

                foreach (var item in DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }
                var list = await context.Orders.SelectAllAsync();
                Assert.AreEqual(DataSource.Count(), list.Count());
            }

        }

        /// <summary>
        /// Asynchronous the test update data.
        /// </summary>
        [Test]
        public async Task AsyncTestUpdateData()
        {
            using (var context = new TestDbContext(nameof(AsyncTestUpdateData)))
            {
                foreach (var item in DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }

                var list = await context.Orders.SelectAsync("CustomerName = @CustomerName", new { CustomerName = "John" });
                foreach (var item in list)
                {
                    item.ShipperCity = "Atlanta";
                    await context.Orders.UpdateAsync(item);
                }

                var updatedList =
                    await context.Orders.SelectAsync("ShipperCity = @ShipperCity", new { ShipperCity = "Atlanta" });
                foreach (var item in updatedList)
                {
                    Assert.AreEqual("Atlanta", item.ShipperCity);
                }
            }
        }

        /// <summary>
        /// Asynchronous the test select data.
        /// </summary>
        [Test]
        public async Task AsyncTestSelectData()
        {
            using (var context = new TestDbContext(nameof(AsyncTestSelectData)))
            {
                foreach (var item in DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }
                // Selecting Data By name
                var selectingData =
                    await context.Orders.SelectAsync("CustomerName = @CustomerName", new { CustomerName = "Peter" });

                foreach (var item in selectingData)
                {
                    Assert.AreEqual("Peter", item.CustomerName);
                }
            }
        }

        /// <summary>
        /// Asynchronous the test count data.
        /// </summary>
        [Test]
        public async Task AsyncTestCountData()
        {
            using (var context = new TestDbContext(nameof(AsyncTestCountData)))
            {
                foreach (var item in DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }

                var selectingData =
                    await context.Orders.SelectAsync("CustomerName = @CustomerName", new { CustomerName = "Peter" });

                Assert.AreEqual(4, selectingData.Count());
            }
        }

        /// <summary>
        /// Asynchronous the test single data.
        /// </summary>
        [Test]
        public async Task AsyncTestSingleData()
        {
            using (var cotext = new TestDbContext(nameof(AsyncTestSingleData)))
            {
                foreach (var item in DataSource)
                {
                    await cotext.Orders.InsertAsync(item);
                }

                var singleSelect = await cotext.Orders.SingleAsync(3);
                Assert.AreEqual("Margarita", singleSelect.CustomerName);
            }
        }

        /// <summary>
        /// Asynchronous the test query data.
        /// </summary>
        [Test]
        public async Task AsyncTestQueryData()
        {
            using (var context = new TestDbContext(nameof(AsyncTestQueryData)))
            {
                foreach (var item in DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }

                var selectedData =
                    await
                        context.Orders.QueryAsync(
                            $"{context.Orders.SelectDefinition} WHERE CustomerName = @CustomerName",
                            new Order { CustomerName = "John" });

                foreach (var item in selectedData)
                {
                    Assert.IsTrue(item.CustomerName == "John");
                }
            }
        }

        /// <summary>
        /// Tests the entity unique.
        /// </summary>
        [Test]
        public void TestEntityUnique()
        {
            using (var context = new TestDbContext(nameof(TestEntityUnique)))
            {
                foreach (var item in DataSource)
                {
                    context.Orders.Insert(item);
                }

                Assert.Throws<SqliteException>(delegate
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

        /// <summary>
        /// Tests the string length.
        /// </summary>
        [Test]
        public void TestStringLengt()
        {
            using (var context = new TestDbContext(nameof(TestStringLengt)))
            {
                Assert.Throws<SqliteException>(delegate
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

        [Test]
        public void TestSets()
        {
            using (var context = new TestDbContext(nameof(TestSets)))
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
        public void TestInsertRangeData()
        {
            using (var context = new TestDbContext(nameof(TestInsertRangeData)))
            {
                context.Orders.InsertRange(DataSource);
                var list = context.Orders.SelectAll();
                Assert.AreEqual(DataSource.Length, list.Count());
            }
        }
    }
}