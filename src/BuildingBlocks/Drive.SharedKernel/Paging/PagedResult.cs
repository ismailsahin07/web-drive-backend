namespace Drive.SharedKernel.Paging
{
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, string? ContinuationToken)
    {
        public bool HasMore => ContinuationToken is not null;
    }
}
