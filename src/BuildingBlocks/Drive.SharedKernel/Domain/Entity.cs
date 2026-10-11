namespace Drive.SharedKernel.Domain
{
    public abstract class Entity<TId> : IEquatable<Entity<TId>>
        where TId : struct, IStronglyTypedId<TId>
    {
        protected Entity(TId id)
        {
            if (id.Value == Guid.Empty)
            {
                throw new ArgumentException("An entity must have a non-empty ID.", nameof(id));
            }

            Id = id;
        }

        public TId Id { get; }

        public bool Equals(Entity<TId>? other) =>
            other is not null
            && other.GetType() == GetType()
            && Id.Equals(other.Id);

        public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(GetType(), Id);

        public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
    }
}
