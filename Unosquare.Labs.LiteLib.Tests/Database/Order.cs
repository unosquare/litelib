using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosquare.Labs.LiteLib.Tests.Database
{
    class Order : LiteModel
    {
        [LiteUnique]
        public string UniqueId { get; set; }

        [LiteIndex]
        public string CustomerName { get; set; }

        public string ShipperCity { get; set; }
        public string IsShipped { get; set; }

        public string Amount { get; set; }
        public string ShippedDate { get; set; }
    }
}
