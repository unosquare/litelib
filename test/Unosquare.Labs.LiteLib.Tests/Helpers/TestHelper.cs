using System;
using System.IO;
using Unosquare.Labs.LiteLib.Tests.Database;

namespace Unosquare.Labs.LiteLib.Tests.Helpers
{
    internal static class TestHelper
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

        internal static string GetTempDb(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}-{name}.db");

            if (File.Exists(path))
                File.Delete(path);

            return path;
        }
    }
}
