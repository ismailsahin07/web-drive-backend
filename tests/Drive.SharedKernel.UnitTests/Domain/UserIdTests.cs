using Drive.SharedKernel.Domain;

namespace Drive.SharedKernel.UnitTests.Domain
{
    public sealed class UserIdTests
    {
        [Fact]
        public void From_ValidGuid_WrapsValue()
        {
            var guid = Guid.NewGuid();

            var userId = UserId.From(guid);

            userId.Value.ShouldBe(guid);
        }

        [Fact]
        public void From_EmptyGuid_Throws()
        {
            Should.Throw<ArgumentException>(() => UserId.From(Guid.Empty));
        }

        [Fact]
        public void UserIds_WithSameGuid_AreEqual()
        {
            var guid = Guid.NewGuid();

            UserId.From(guid).ShouldBe(UserId.From(guid));
        }

        [Fact]
        public void ToString_ReturnsGuidOnly()
        {
            var guid = Guid.NewGuid();

            UserId.From(guid).ToString().ShouldBe(guid.ToString());
        }
    }
}
