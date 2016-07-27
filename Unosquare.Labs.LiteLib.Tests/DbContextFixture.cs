using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Labs.LiteLib.Tests.Database;

namespace Unosquare.Labs.LiteLib.Tests
{
    [TestFixture]
    public class DbContextFixture
    {
        private readonly Order[] _sampleData =
        {
            new Order {UniqueId = "1"},
            new Order {UniqueId = "2"},
        };
        
        [Test]
        public void TestSelectAll()
        {
            using (var context = new TestDbContext(nameof(TestSelectAll)))
            {
                foreach (var item in _sampleData)
                {
                    context.Orders.Insert(item);
                }

                var list = context.Orders.SelectAll();

                Assert.AreEqual(_sampleData.Count(), list.Count(), "Same set");
            }
        }

        //Test Delete Method
        [Test]
        public void TestDeleteData()
        {
            using (var context = new TestDbContext(nameof(TestDeleteData)))
            {
                var loadData = new List<Order>();
                for (var i = 0; i < 10; i++)
                {
                    loadData.Add(new Order {CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4"});
                    loadData.Add(new Order {CustomerName = "John", ShipperCity = "Leon", Amount = "6"});
                    loadData.Add(new Order {CustomerName = "John", ShipperCity = "Boston", Amount = "7"});
                }

                foreach (var item in loadData)
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

        //Test Insert method
        [Test]
        public void TestInsertData()
        {
            using (var _context = new TestDbContext(nameof(TestInsertData)))
            {
                while (_context.Orders.Count() != 0)
                {
                    var incomingData = _context.Orders.SelectAll();
                    foreach (var item in incomingData)
                    {
                        _context.Orders.Delete(item);
                    }
                }
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }

                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }
                var list = _context.Orders.SelectAll();

                Assert.AreEqual(dataSource.Count(), list.Count());
            }
            
        }

        // Test Update method
        [Test]
        public void TestUpdateData()
        {
            using (var _context = new TestDbContext(nameof(TestUpdateData)))
            {
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }

                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }

                var list = _context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "John" });
                foreach (var item in list)
                {
                    item.ShipperCity = "Atlanta";
                    _context.Orders.Update(item);
                }
                var updatedList = _context.Orders.Select("ShipperCity = @ShipperCity", new { ShipperCity = "Atlanta" });
                foreach (var item in updatedList)
                {
                    Assert.AreEqual("Atlanta", item.ShipperCity);
                }
            }            
        }

        //Test Select method
        [Test]
        public void TestSelectData()
        {
            using (var _context = new TestDbContext(nameof(TestSelectData)))
            {
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }
                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }
                // Selecting Data By name
                var selectingData = _context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Peter" });

                foreach (var item in selectingData)
                {
                    Assert.AreEqual("Peter", item.CustomerName);
                }
            }
        }

        [Test]
        public void TestCountData()
        {
            using (var _context = new TestDbContext(nameof(TestCountData)))
            {
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }
                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }
                var selectingData = _context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Peter" });
                Assert.AreEqual(10, selectingData.Count());
            }
        }

        //Test OnBeforeInsert
        [Test]
        public void OnBeforeInsertTest()
        {
            using (var _context = new TestDbContext(nameof(OnBeforeInsertTest)))
            {
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }

                _context.Orders.OnBeforeInsert += (s, e) =>
                {
                    if (e.Entity.CustomerName == "Peter")
                    {
                        e.Entity.CustomerName = "Charles";
                    }
                };

                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }

                var updatedList = _context.Orders.Select("ShipperCity = @ShipperCity", new { ShipperCity = "Leon" });
                foreach (var item in updatedList)
                {
                    Assert.AreNotEqual("Peter", item.CustomerName);
                }
            }
        }

        // Test OnAfterInsert
        [Test]
        public void OnAfterInsert()
        {
            using (var _context = new TestDbContext(nameof(OnAfterInsert)))
            {
                //Deleting default elements in the table
                var incomingData = _context.Orders.SelectAll();
                foreach (var item in incomingData)
                {
                    _context.Orders.Delete(item);
                }
                //Begining with the Test
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }

                _context.Orders.OnAfterInsert += (s, e) =>
                {
                    if (e.Entity.CustomerName == "John" || e.Entity.CustomerName == "Peter")
                    {
                        _context.Orders.Delete(e.Entity);
                    }
                };

                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }
                var afterList = _context.Orders.SelectAll();
                foreach (var item in afterList)
                {
                    Assert.AreEqual("Margarita", item.CustomerName);
                }

                Assert.AreEqual(10, afterList.Count());
            }
        }

        //Test OnBeforeUpdate
        [Test]
        public void OnBeforeUpdateTest()
        {
            using (var _context = new TestDbContext(nameof(OnBeforeUpdateTest)))
            {
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }

                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }

                _context.Orders.OnBeforeUpdate += (s, e) =>
                {
                    if (e.Entity.ShipperCity == "Leon")
                    {
                        e.Entity.ShipperCity = "Atlanta";
                    }
                };

                foreach (var item in _context.Orders.SelectAll())
                {
                    _context.Orders.Update(item);
                }

                var updatedList = _context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Peter" });
                foreach (var item in updatedList)
                {
                    Assert.AreEqual("Atlanta", item.ShipperCity);
                }
            }
        }

        //Test OnAfterUpdate
        [Test]
        public void OnAfterUpdateTest()
        {
            using (var _context = new TestDbContext(nameof(OnAfterUpdateTest)))
            {
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }

                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }

                var newDataSource = new List<Order>();
                _context.Orders.OnAfterUpdate += (s, e) =>
                {
                    if (e.Entity.ShipperCity == "Guadalajara")
                    {
                        newDataSource.Add(e.Entity);
                    }
                };

                foreach (var item in _context.Orders.SelectAll())
                {
                    _context.Orders.Update(item);
                }
                Assert.AreEqual(10, newDataSource.Count());
            }
        }

        //Test OnBeforeDelete
        [Test]
        public void OnBeforeDeleteTest()
        {
            using (var _context = new TestDbContext(nameof(OnBeforeDeleteTest)))
            {
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }

                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }
                var deletedList = new List<Order>();
                _context.Orders.OnBeforeDelete += (s, e) =>
                {
                    deletedList.Add(e.Entity);
                };
                foreach (var item in _context.Orders.SelectAll())
                {
                    if (item.CustomerName == "John")
                    {
                        _context.Orders.Delete(item);
                    }
                }

                Assert.AreEqual(10, deletedList.Count());
            }
        }

        //Test OnAfterDelete
        [Test]
        public void OnAfterDeleteTest()
        {
            using (var _context = new TestDbContext(nameof(OnAfterDeleteTest)))
            {
                var dataSource = new List<Order>();

                for (var i = 0; i < 10; i++)
                {
                    dataSource.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                    dataSource.Add(new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" });
                    dataSource.Add(new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" });
                }

                foreach (var item in dataSource)
                {
                    _context.Orders.Insert(item);
                }

                _context.Orders.OnAfterDelete += (s, e) =>
                {
                    e.Entity.CustomerName = "Jessy";
                    _context.Orders.Insert(e.Entity);
                };
                foreach (var item in _context.Orders.SelectAll())
                {
                    if (item.CustomerName == "Margarita")
                    {
                        _context.Orders.Delete(item);
                    }
                }

                foreach (var item in _context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Jessy" }))
                {
                    Assert.AreEqual("Jessy", item.CustomerName);
                }
            }
        }
    }
}
