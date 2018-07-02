using System;
using System.ComponentModel.DataAnnotations;

namespace Unosquare.Labs.LiteLib.Tests.Database
{
    internal class Order : LiteModel
    {
        [LiteUnique]
        public string UniqueId { get; set; }

        [LiteIndex]
        public string CustomerName { get; set; }

        [StringLength(30)]
        public string ShipperCity { get; set; }

        public bool IsShipped { get; set; }

        public int Amount { get; set; }

        public DateTime? ShippedDate { get; set; }

        public ComplexDetail ComplexDetail { get; set; }
    }
}