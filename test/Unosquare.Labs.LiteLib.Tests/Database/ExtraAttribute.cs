namespace Unosquare.Labs.LiteLib.Tests.Database
{
    using Unosquare.Labs.LiteLib.Tests.Helpers;

    [CustomAttribute("MyCustom", version = 1.0)]
    class ExtraAttribute : LiteModel
    {
        [LiteUnique]
        public string UniqueId { get; set; }

        [LiteIndex]
        public string ExraName { get; set; }
    }
}
