namespace Unosquare.Labs.LiteLib.Tests
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using Database;
    using Helpers;

    [TestFixture]
    public class DbContextEventsFixture
    {
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

                foreach (var item in TestHelper.DataSource)
                {
                    context.Orders.Insert(item);
                }

                var updatedList = context.Orders
                    .Select("ShipperCity = @ShipperCity", new { ShipperCity = "Leon" });

                foreach (var item in updatedList)
                {
                    Assert.AreNotEqual("Peter", item.CustomerName);
                }
            }
        }

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

            foreach (var item in TestHelper.DataSource)
            {
                context.Orders.Insert(item);
            }

            var afterList = context.Orders.SelectAll().ToList();

            foreach (var item in afterList)
            {
                Assert.AreEqual("Margarita", item.CustomerName);
            }

            Assert.AreEqual(4, afterList.Count);
        }

        [Test]
        public void OnBeforeUpdateTest()
        {
            using (var context = new TestDbContext(nameof(OnBeforeUpdateTest)))
            {
                foreach (var item in TestHelper.DataSource)
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

                context.Orders
                    .Select("CustomerName = @CustomerName", new { CustomerName = "Peter" })
                    .ToList()
                    .ForEach(x => Assert.AreEqual("Atlanta", x.ShipperCity));
            }
        }

        [Test]
        public void OnAfterUpdateTest()
        {
            using (var context = new TestDbContext(nameof(OnAfterUpdateTest)))
            {
                var entity = TestHelper.DataSource.First();

                context.Orders.Insert(entity);
                var changed = false;

                context.Orders.OnAfterUpdate += (s, e) => changed = true;

                entity.Amount = 10;

                context.Orders.Update(entity);

                Assert.IsTrue(changed);
            }
        }

        [Test]
        public void OnBeforeDeleteTest()
        {
            using (var context = new TestDbContext(nameof(OnBeforeDeleteTest)))
            {
                foreach (var item in TestHelper.DataSource)
                {
                    context.Orders.Insert(item);
                }

                var deletedList = new List<Order>();
                context.Orders.OnBeforeDelete += (s, e) => deletedList.Add(e.Entity);

                foreach (var item in context.Orders.SelectAll().Where(item => item.CustomerName == "John"))
                {
                    context.Orders.Delete(item);
                }

                Assert.AreEqual(4, deletedList.Count);
            }
        }

        [Test]
        public void OnAfterDeleteTest()
        {
            var context = new TestDbContext(nameof(OnAfterDeleteTest));
            foreach (var item in TestHelper.DataSource)
            {
                context.Orders.Insert(item);
            }

            context.Orders.OnAfterDelete += (s, e) =>
            {
                e.Entity.CustomerName = "Jessy";
                context.Orders.Insert(e.Entity);
            };

            foreach (var item in context.Orders.SelectAll().Where(item => item.CustomerName == "Margarita"))
            {
                context.Orders.Delete(item);
            }

            foreach (var item in context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Jessy" }))
            {
                Assert.AreEqual("Jessy", item.CustomerName);
            }
        }

        [Test]
        public void TestEntityEventArgs()
        {
            using (var context = new TestDbContext(nameof(TestEntityEventArgs)))
            {
                EntityEventArgs<Order> eventOrder = null;
                var entity = TestHelper.DataSource.First();
                context.Orders.OnAfterInsert += (s, e) => eventOrder = e;
                context.Orders.Insert(entity);

                Assert.IsNotNull(eventOrder);
                Assert.IsFalse(eventOrder.Cancel);
                Assert.AreEqual(context.Orders, eventOrder.DbSet, "Same DbSet");
                Assert.AreEqual(entity.RowId, eventOrder.Entity.RowId, "Same Entity Row");
            }
        }
    }
}