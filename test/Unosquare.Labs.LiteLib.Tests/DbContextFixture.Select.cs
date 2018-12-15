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
        public class Select : DbContextFixture
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

                    var entities = context.Orders.Select("CustomerName = @CustomerName", new {CustomerName = "John"});
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

#if NET452
                    Assert.Throws<Mono.Data.Sqlite.SqliteException>(() =>
#else
                    Assert.Throws<SqliteException>(() =>
#endif
                    {
                        context.Orders.Select("Customer = @CustomerName", new {CustomerName = "John"});
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

            [Test]
            public void SelectingFirstOrDefaultLambda()
            {
                using (var context = new TestDbContext(nameof(SelectingFirstOrDefault)))
                {
                    var id = context.Orders.Insert(TestHelper.DataSource.First());
                    Assert.AreNotEqual(0, id);

                    var order = context.Orders.FirstOrDefault(x => x.CustomerName, "John");

                    Assert.IsNotNull(order);
                }
            }
        }
    }
}