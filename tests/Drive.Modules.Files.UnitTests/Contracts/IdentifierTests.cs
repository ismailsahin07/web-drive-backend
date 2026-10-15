using Drive.Modules.Files.Contracts;

namespace Drive.Modules.Files.UnitTests.Contracts
{
    public sealed class IdentifierTests
    {
        [Fact]
        public void ItemId_New_CreatesNonEmptyUniqueIds()
        {
            var first = ItemId.New();
            var second = ItemId.New();

            first.Value.ShouldNotBe(Guid.Empty);
            first.ShouldNotBe(second);
        }

        [Fact]
        public void ItemId_From_EmptyGuid_Throws()
        {
            Should.Throw<ArgumentException>(() => ItemId.From(Guid.Empty));
        }

        [Fact]
        public void ItemId_From_RoundTripsValue()
        {
            var guid = Guid.NewGuid();

            ItemId.From(guid).Value.ShouldBe(guid);
        }

        [Fact]
        public void DriveId_New_CreatesNonEmptyUniqueIds()
        {
            var first = DriveId.New();
            var second = DriveId.New();

            first.Value.ShouldNotBe(Guid.Empty);
            first.ShouldNotBe(second);
        }

        [Fact]
        public void DriveId_From_EmptyGuid_Throws()
        {
            Should.Throw<ArgumentException>(() => DriveId.From(Guid.Empty));
        }
    }
}
