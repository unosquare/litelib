namespace Unosquare.Labs.LiteLib.Tests
{
    using Database;
    using Helpers;
    using NUnit.Framework;
#if !NET461
#endif

    public partial class DbContextFixture
    {
        public class AnyTest : DbContextAsyncFixture
        {
            [Test]
            public void AnyMethod_ShouldPass()
            {
                using (var context = new TestDbContext(nameof(AnyMethod_ShouldPass)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var result = context.Orders.Any();

                    Assert.IsTrue(result);
                }
            }

            [Test]
            public void AnyMethodWithParams_ShouldPass()
            {
                using (var context = new TestDbContext(nameof(AnyMethodWithParams_ShouldPass)))
                {
                    foreach (var item in TestHelper.DataSource)
                    {
                        context.Orders.Insert(item);
                    }

                    var result = context.Orders.Any("CustomerName = @CustomerName", new {CustomerName = "John"});

                    Assert.IsTrue(result);
                }
            }

            [Test]
            public void AnyMethodWithParams_ShouldFail()
            {
                using (var context = new TestDbContext(nameof(AnyMethodWithParams_ShouldFail)))
                {
                    var result = context.Orders.Any("CustomerName", "Fail");

                    Assert.IsFalse(result);
                }
            }
        }
    }
}