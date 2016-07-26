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
        private TestDbContext _context;

        private readonly Order[] _sampleData =
        {
            new Order {UniqueId = "1"},
            new Order {UniqueId = "2"},
        };

        [SetUp]
        public void Init()
        {
            _context = new TestDbContext();

            foreach (var item in _sampleData)
            {
                _context.Orders.Insert(item);
            }
        }

        [Test]
        public void TestSelectAll()
        {
            var list = _context.Orders.SelectAll();

            Assert.AreEqual(_sampleData.Count(), list.Count(), "Same set");
        }

        // TODO: Test methods

        //Test Delete Method
        [Test]
        public void TestDeleteData()
        {
            var loadData = new List<Order>();
            for (var i = 0; i<10 ;i++)
            {
                loadData.Add(new Order { CustomerName = "John", ShipperCity = "Guadalajara", Amount = "4" });
                loadData.Add(new Order { CustomerName = "John", ShipperCity = "Leon", Amount = "6" });
                loadData.Add(new Order { CustomerName = "John", ShipperCity = "Boston", Amount = "7" });
            }

            foreach (var item in loadData)
            {
                _context.Orders.Insert(item);
            }
            var incomingData = _context.Orders.SelectAll();
            foreach (var item in incomingData)
            {
                _context.Orders.Delete(item);
            }

            Assert.AreEqual(0,_context.Orders.Count());
        }

        //Test Insert method
        [Test]
        public void TestInsertData()
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

            for(var i=0 ;i<10;i++)
            {
                dataSource.Add(new Order { CustomerName ="John", ShipperCity="Guadalajara", Amount="4"});
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

        // Test Update method
        [Test]
        public void TestUpdateData()
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
            
            var list = _context.Orders.Select("CustomerName = @CustomerName",new { CustomerName = "John"});
            foreach(var item in list)
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

        //test Select method
        [Test]
        public void TestSelectData()
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
}
