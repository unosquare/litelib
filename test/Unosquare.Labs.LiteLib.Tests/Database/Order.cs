using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [StringLength(30)]
        public string ShipperCity { get; set; }
        public bool IsShipped { get; set; }
        public int Amount { get; set; }
        public string ShippedDate { get; set; }

        public ComplexDetail ComplexDetail { get; set; }
    }
}
