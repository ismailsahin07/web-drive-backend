namespace Drive.SharedKernel.Messaging
{
    public interface IIntegrationEvent
    {
        Guid Id { get; }

        DateTimeOffset OccurredAt { get; }
    }
}
