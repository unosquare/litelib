namespace Unosquare.Labs.LiteLib.Tests.Database
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("ComplexDetails")]
    internal class ComplexDetail : LiteModel
    {
        [LiteUnique]
        public int Id { get; set; }
    }
}