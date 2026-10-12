namespace Drive.SharedKernel.Messaging
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
            where TEvent : IIntegrationEvent;
    }
}
