using Drive.Modules.Files.Application.Items;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class PageSizesTests
    {
        [Theory]
        [InlineData(null, PageSizes.Default)]
        [InlineData(10, 10)]
        [InlineData(1, 1)]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(PageSizes.Max, PageSizes.Max)]
        [InlineData(1000, PageSizes.Max)]
        public void Resolve_ClampsToAllowedRange(int? requested, int expected)
        {
            PageSizes.Resolve(requested).ShouldBe(expected);
        }
    }
}
