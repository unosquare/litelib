using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unosquare.Labs.LiteLib.Tests.Database;
using Unosquare.Labs.LiteLib.Tests.Helpers;
#if MONO
    using Mono.Data.Sqlite;
#else
using Microsoft.Data.Sqlite;
#endif

namespace Unosquare.Labs.LiteLib.Tests
{
    public class Program
    {
        static void Main(string[] args)
        {
            var x = new DbContextFixture();
            x.TestFirstOrDefault();
        }
    }

    /// <summary>
    /// A TestFixture to test the included methods in LiteDbSet
    /// </summary>
    [TestFixture]
    public class DbContextFixture
    {
        /// <summary>
        /// Tests the select all.
        /// </summary>
        [Test]
        public void TestSelectAll()
        {
            using (var context = new TestDbContext(nameof(TestSelectAll)))
            {
                foreach (var item in TestHelper.DataSource)
                {
                    context.Orders.Insert(item);
                }

                var list = context.Orders.SelectAll();
                Assert.AreEqual(TestHelper.DataSource.Length, list.Count(), "Same set");
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

        /// <summary>
        /// Tests the insert data.
        /// </summary>
        [Test]
        public void TestInsertData()
        {
            using (var context = new TestDbContext(nameof(TestInsertData)))
            {
                var entity = TestHelper.DataSource.First();
                context.Orders.Insert(entity);
                
                Assert.AreNotEqual(0, entity.RowId, "Has a RowId value");
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
                foreach (var item in TestHelper.DataSource)
                {
                    context.Orders.Insert(item);
                }

                var list = context.Orders.Select("CustomerName = @CustomerName", new {CustomerName = "John"});
                foreach (var item in list)
                {
                    item.ShipperCity = "Atlanta";
                    context.Orders.Update(item);
                }

                var updatedList = context.Orders.Select("ShipperCity = @ShipperCity", new {ShipperCity = "Atlanta"});
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
                foreach (var item in TestHelper.DataSource)
                {
                    context.Orders.Insert(item);
                }

                var selectingData = context.Orders.Select("CustomerName = @CustomerName", new {CustomerName = "Peter"});
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
                foreach (var item in TestHelper.DataSource)
                {
                    context.Orders.Insert(item);
                }

                var selectingData = context.Orders.Select("CustomerName = @CustomerName",
                    new {CustomerName = "Margarita"});
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
                    foreach (var item in TestHelper.DataSource)
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
                    Assert.IsTrue(item.CustomerName == "Margarita");
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
                foreach (var item in TestHelper.DataSource)
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
                Assert.AreEqual(names, new[] {nameof(context.Orders), nameof(context.Warehouses)});

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
                context.Orders.InsertRange(TestHelper.DataSource);
                var list = context.Orders.SelectAll();
                Assert.AreEqual(TestHelper.DataSource.Length, list.Count());
            }
        }

        [Test]
        public void TestInsertRangeEmptyset()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                using (var context = new TestDbContext(nameof(TestInsertRangeData)))
                {
                    context.Orders.InsertRange(new List<Order>());
                }
            });
        }

        [Test]
        public void TestSelectFromSetname()
        {
            using (var context = new TestDbContext(nameof(TestSets)))
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
        public void TestInvalidSetname()
        {
            Assert.Throws(typeof(System.ArgumentOutOfRangeException), () =>
            {
                using (var context = new TestDbContext(nameof(TestInvalidSetname)))
                {
                    context.Set<Program>();
                }
            });
        }

        [Test]
        public void TestInsertFromSetname()
        {
            using (var context = new TestDbContext(nameof(TestInsertFromSetname)))
            {
                foreach (var item in TestHelper.DataSource)
                {
                    context.Insert(item);
                }

                Assert.AreEqual(TestHelper.DataSource.Length, context.Orders.Count(), "Has data");
            }
        }
        
        [Test]
        public void TestDeleteFromSetname()
        {
            using (var context = new TestDbContext(nameof(TestDeleteFromSetname)))
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
        public void TestFirstOrDefault()
        {
            using (var context = new TestDbContext(nameof(TestFirstOrDefault)))
            {
                var id = context.Orders.Insert(TestHelper.DataSource.First());
                Assert.AreNotEqual(0, id);

                var order = context.Orders.FirstOrDefault(nameof(Order.CustomerName), "John");

                Assert.IsNotNull(order);
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
                var updatedItems = context.Orders.Select("ShipperCity = @ShipperCity", new { ShipperCity = "Atlanta" });
                Assert.AreEqual(TestHelper.DataSource.Length, updatedItems.Count());
            }
        }
    }
}