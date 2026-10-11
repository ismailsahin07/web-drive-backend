namespace Drive.SharedKernel.Results;

public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("General Validation", "One or more validation errors occured.", ErrorType.Validation);
