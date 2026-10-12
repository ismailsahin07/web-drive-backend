using Drive.SharedKernel.Domain;

namespace Drive.SharedKernel.UnitTests.TestDoubles
{
    internal sealed class TestAggregate(TestId id) : AggregateRoot<TestId>(id)
    {
        public void DoSomething(DateTimeOffset now) => Raise(new TestDomainEvent(now));

        public void RaiseEvent(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    internal sealed record TestDomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;
}
