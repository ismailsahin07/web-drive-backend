using Drive.SharedKernel.UnitTests.TestDoubles;

namespace Drive.SharedKernel.UnitTests.Domain
{
    public sealed class EntityTests
    {
        [Fact]
        public void Constructor_WithEmptyId_Throws()
        {
            Should.Throw<ArgumentException>(() => new TestEntity(default));
        }

        [Fact]
        public void Entities_WithSameId_AreEqual()
        {
            var id = TestId.New();
            var first = new TestEntity(id);
            var second = new TestEntity(id);

            first.Equals(second).ShouldBeTrue();
            (first == second).ShouldBeTrue();
            (first != second).ShouldBeFalse();
            first.GetHashCode().ShouldBe(second.GetHashCode());
        }

        [Fact]
        public void Entities_WithDifferentIds_AreNotEqual()
        {
            var first = new TestEntity(TestId.New());
            var second = new TestEntity(TestId.New());

            first.Equals(second).ShouldBeFalse();
            (first == second).ShouldBeFalse();
            (first != second).ShouldBeTrue();
        }

        [Fact]
        public void Entities_OfDifferentTypes_WithSameId_AreNotEqual()
        {
            var id = TestId.New();
            var entity = new TestEntity(id);
            var other = new OtherTestEntity(id);

            entity.Equals(other).ShouldBeFalse();
            (entity == other).ShouldBeFalse();
        }

        [Fact]
        public void Entity_ComparedWithNull_IsNotEqual()
        {
            var entity = new TestEntity(TestId.New());
            TestEntity? nothing = null;

            entity.Equals(nothing).ShouldBeFalse();
            (entity == nothing).ShouldBeFalse();
            (nothing == entity).ShouldBeFalse();
        }

        [Fact]
        public void TwoNullEntities_AreEqual()
        {
            TestEntity? first = null;
            TestEntity? second = null;

            (first == second).ShouldBeTrue();
        }
    }
}
