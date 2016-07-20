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
    }
}
