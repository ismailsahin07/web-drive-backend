namespace Drive.SharedKernel.Domain
{
    public interface IDomainEvent
    {
        DateTimeOffset OccurredAt { get; }
    }
}
