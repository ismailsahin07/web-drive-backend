using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Abstractions
{
    internal interface IUnitOfWork
    {
        /// <summary>
        /// Saves everything added or updated through the repositories as one atomic operation.
        /// Fails with <see cref="FilesErrors.ConcurrencyConflict"/> if any document was changed by someone
        /// else since it was loaded, or a newly added document already exists; and with
        /// <see cref="FilesErrors.NameTaken"/> if a name is already in use.
        /// </summary>
        Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
