using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Unosquare.Labs.LiteLib.Tests.Database;
using Unosquare.Labs.LiteLib.Tests.Helpers;

namespace Unosquare.Labs.LiteLib.Tests
{
    /// <summary>
    /// A TestFixture to test the included async methods in LiteDbSet
    /// </summary>
    [TestFixture]
    public class DbContextAsyncFixture
    {

        /// <summary>
        /// Asynchronous the test select all.
        /// </summary>
        [Test]
        public async Task AsyncTestSelectAll()
        {
            using (var context = new TestDbContext(nameof(AsyncTestSelectAll)))
            {
                foreach (var item in TestHelper.DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }

                var list = await context.Orders.SelectAllAsync();
                Assert.AreEqual(TestHelper.DataSource.Count(), list.Count(), "Same set");
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

                foreach (var item in TestHelper.DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }
                var list = await context.Orders.SelectAllAsync();
                Assert.AreEqual(TestHelper.DataSource.Count(), list.Count());
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
                foreach (var item in TestHelper.DataSource)
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
                foreach (var item in TestHelper.DataSource)
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
                foreach (var item in TestHelper.DataSource)
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
                foreach (var item in TestHelper.DataSource)
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
                foreach (var item in TestHelper.DataSource)
                {
                    await context.Orders.InsertAsync(item);
                }

                var selectedData =
                    await
                        context.QueryAsync<Order>(
                            $"{context.Orders.SelectDefinition} WHERE CustomerName = @CustomerName",
                            new Order { CustomerName = "John" });

                foreach (var item in selectedData)
                {
                    Assert.IsTrue(item.CustomerName == "John");
                }
            }
        }

        [Test]
        public async Task AsyncTestInsertFromSetname()
        {
            using (var context = new TestDbContext(nameof(AsyncTestInsertFromSetname)))
            {
                foreach (var item in TestHelper.DataSource)
                {
                    await context.InsertAsync(item);
                }

                Assert.AreEqual(TestHelper.DataSource.Length, context.Orders.Count(), "Has data");
            }
        }

        [Test]
        public async Task AsyncTestDeleteFromSetname()
        {
            using (var context = new TestDbContext(nameof(AsyncTestDeleteFromSetname)))
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
    }
}
