namespace Unosquare.Labs.LiteLib.Tests.Database
{
    [Table("ComplexDetails")]
    internal class ComplexDetail : LiteModel
    {
        [LiteUnique]
        public int Id { get; set; }
    }
}