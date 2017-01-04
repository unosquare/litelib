using System.ComponentModel.DataAnnotations.Schema;

namespace Unosquare.Labs.LiteLib.Tests.Database
{
    [Table("ComplexDetails")]
    class ComplexDetail : LiteModel
    {
        [LiteUnique]
        public int Id { get; set; }
    }
}
