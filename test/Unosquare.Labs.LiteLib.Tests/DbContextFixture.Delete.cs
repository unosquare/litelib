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
        public class Delete : DbContextFixture
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
                        context.Orders.Delete("CustomerName = @CustomerName", new {CustomerName = "Peter"});
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

#if NET452
                    Assert.Throws<Mono.Data.Sqlite.SqliteException>(() =>
#else
                    Assert.Throws<SqliteException>(() =>
#endif
                    {
                        context.Orders.Delete("Customer = @CustomerName", new {CustomerName = "Peter"});
                    });
                }
            }
        }
    }
}