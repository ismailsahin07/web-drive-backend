namespace Drive.SharedKernel.Domain
{
    public abstract class AggregateRoot<TId> : Entity<TId>
        where TId : struct, IStronglyTypedId<TId>
    {
        private readonly List<IDomainEvent> _domainEvents = [];

        protected AggregateRoot(TId id)
            : base(id)
        {
        }

        public string? ConcurrencyToken { get; private set; }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void SetConcurrencyToken(string? token) => ConcurrencyToken = token;

        public IReadOnlyList<IDomainEvent> PullDomainEvents()
        {
            var events = _domainEvents.ToArray();
            _domainEvents.Clear();
            return events;
        }

        protected void Raise(IDomainEvent domainEvent)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            _domainEvents.Add(domainEvent);
        }
    }
}
