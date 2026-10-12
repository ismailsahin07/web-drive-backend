using Drive.SharedKernel.Domain;

namespace Drive.SharedKernel.Abstractions
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        UserId Id { get; }

        string? Email { get; }

        string? DisplayName { get; }
    }
}
