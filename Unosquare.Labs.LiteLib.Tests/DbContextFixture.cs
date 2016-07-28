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
        private readonly Order[] dataSource =
        {
             new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" },
             new Order { CustomerName = "Peter", ShipperCity = "Leon", Amount = "6" },
             new Order { CustomerName = "Margarita", ShipperCity = "Boston", Amount = "7" }
        };

        [Test]
        public void TestSelectAll()
        {
            using (var context = new TestDbContext(nameof(TestSelectAll)))
            {
                foreach (var item in dataSource)
                {
                    context.Orders.Insert(item);
                }

                var list = context.Orders.SelectAll();

                Assert.AreEqual(dataSource.Count(), list.Count(), "Same set");
            }
        }

        //Test Delete Method
        [Test]
        public void TestDeleteData()
        {
            using (var context = new TestDbContext(nameof(TestDeleteData)))
            {

                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        context.Orders.Insert(item);
                    }
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

        //Test Count method
        [Test]
        public void TestCountData()
        {
            using (var _context = new TestDbContext(nameof(TestCountData)))
            {
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        _context.Orders.Insert(item);
                    }
                }

                var selectingData = _context.Orders.Select("CustomerName = @CustomerName", new { CustomerName = "Margarita" });
                Assert.AreEqual(10, selectingData.Count());
            }
        }
        //Test Single method
        [Test]
        public void TestSingleData()
        {
            using (var _cotext = new TestDbContext(nameof(TestSingleData)))
            {
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        _cotext.Orders.Insert(item);
                    }
                }
                var singleSelect = _cotext.Orders.Single(3);
                Assert.AreEqual("Margarita", singleSelect.CustomerName);


            }
        }

        //Test Query method
        [Test]
        public void TestQueryData()
        {
            using (var _context = new TestDbContext(nameof(TestQueryData)))
            {
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        _context.Orders.Insert(item);
                    }
                }

                var selectedData = _context.Orders.Query($"{_context.Orders.SelectDefinition} WHERE CustomerName = @CustomerName", new Order { CustomerName = "Margarita" });
                foreach (var item in selectedData)
                {
                    Assert.IsTrue(item.CustomerName == "Margarita");
                }


            }
        }
        

        //Test OnBeforeInsert
        [Test]
        public void OnBeforeInsertTest()
        {
            using (var _context = new TestDbContext(nameof(OnBeforeInsertTest)))
            {
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
                _context.Orders.OnAfterInsert += (s, e) =>
                {
                    if (e.Entity.CustomerName == "John" || e.Entity.CustomerName == "Peter")
                    {
                        _context.Orders.Delete(e.Entity);
                    }
                };
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        _context.Orders.Insert(item);
                    }
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

                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in _context.Orders.SelectAll())
                    {
                        _context.Orders.Update(item);
                    }
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
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        _context.Orders.Insert(item);
                    }
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

        //Async Test Methods
        //Async SelectAll
        [Test]
        public async void AsyncTestSelectAll()
        {
            using (var context = new TestDbContext(nameof(AsyncTestSelectAll)))
            {
                foreach (var item in dataSource)
                {
                    await context.Orders.InsertAsync(item);
                }

                var list = await context.Orders.SelectAllAsync();

                Assert.AreEqual(dataSource.Count(), list.Count(), "Same set");
            }
        }
        //Test Async Delete Method
        [Test]
        public async void AsyncTestDeleteData()
        {
            using (var context = new TestDbContext(nameof(AsyncTestDeleteData)))
            {
               foreach (var item in dataSource)
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

        //Test Async Insert method
        [Test]
        public async void AsyncTestInsertData()
        {
            using (var _context = new TestDbContext(nameof(AsyncTestInsertData)))
            {
                while (_context.Orders.Count() != 0)
                {
                    var incomingData = _context.Orders.SelectAll();
                    foreach (var item in incomingData)
                    {
                        await _context.Orders.DeleteAsync(item);
                    }
                }
                foreach (var item in dataSource)
                {
                    await _context.Orders.InsertAsync(item);
                }
                var list = await  _context.Orders.SelectAllAsync();

                Assert.AreEqual(dataSource.Count(), list.Count());
            }

       }

        // Test Async Update method
        [Test]
        public async void AsyncTestUpdateData()
        {
            using (var _context = new TestDbContext(nameof(AsyncTestUpdateData)))
            {
                foreach (var item in dataSource)
                {
                    await _context.Orders.InsertAsync(item);
                }

                var list = await _context.Orders.SelectAsync("CustomerName = @CustomerName", new { CustomerName = "John" });
                foreach (var item in list)
                {
                    item.ShipperCity = "Atlanta";
                    await _context.Orders.UpdateAsync(item);
                }
                var updatedList =await _context.Orders.SelectAsync("ShipperCity = @ShipperCity", new { ShipperCity = "Atlanta" });
                foreach (var item in updatedList)
                {
                    Assert.AreEqual("Atlanta", item.ShipperCity);
                }
            }
        }

        //Test Async Select method
        [Test]
        public async void AsyncTestSelectData()
        {
            using (var _context = new TestDbContext(nameof(AsyncTestSelectData)))
            {
                foreach (var item in dataSource)
                {
                    await _context.Orders.InsertAsync(item);
                }
                // Selecting Data By name
                var selectingData = await _context.Orders.SelectAsync("CustomerName = @CustomerName", new { CustomerName = "Peter" });

                foreach (var item in selectingData)
                {
                    Assert.AreEqual("Peter", item.CustomerName);
                }
            }
        }

        //Test Async Count method
        [Test]
        public async void AsyncTestCountData()
        {
            using (var _context = new TestDbContext(nameof(AsyncTestCountData)))
            {
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        await _context.Orders.InsertAsync(item);
                    }
                }

                var selectingData = await _context.Orders.SelectAsync("CustomerName = @CustomerName", new { CustomerName = "Peter" });
                Assert.AreEqual(10, selectingData.Count());
            }
        }

        //Test Async Single method
        [Test]
        public async void AsyncTestSingleData()
        {
            using (var _cotext = new TestDbContext(nameof(AsyncTestSingleData)))
            {
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        await _cotext.Orders.InsertAsync(item);
                    }
                }
                var singleSelect = await _cotext.Orders.SingleAsync(3);
                Assert.AreEqual("Margarita", singleSelect.CustomerName);
            }
        }
        //Test Async Query method
        [Test]
        public async void AsyncTestQueryData()
        {
            using (var _context = new TestDbContext(nameof(AsyncTestQueryData)))
            {
                for (var i = 0; i < 10; i++)
                {
                    foreach (var item in dataSource)
                    {
                        await _context.Orders.InsertAsync(item);
                    }
                }

                var selectedData = await _context.Orders.QueryAsync($"{_context.Orders.SelectDefinition} WHERE CustomerName = @CustomerName", new Order { CustomerName = "John" });
                foreach (var item in selectedData)
                {
                    Assert.IsTrue(item.CustomerName == "John");
                }


            }
        }
    }
}
