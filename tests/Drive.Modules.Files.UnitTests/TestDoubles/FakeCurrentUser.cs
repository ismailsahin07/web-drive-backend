using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.UnitTests.TestDoubles
{
    internal sealed class FakeCurrentUser(UserId id) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public UserId Id { get; } = id;

        public string? Email => null;

        public string? DisplayName => null;
    }
}
