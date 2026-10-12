using Drive.SharedKernel.Domain;

namespace Drive.SharedKernel.UnitTests.TestDoubles
{
    internal sealed class TestEntity(TestId id) : Entity<TestId>(id);

    internal sealed class OtherTestEntity(TestId id) : Entity<TestId>(id);
}
