namespace BlazorKompovNet.Models;

public sealed class ClubOperationResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public static ClubOperationResult Success(string message)
    {
        return new ClubOperationResult { Succeeded = true, Message = message };
    }

    public static ClubOperationResult Failure(string message)
    {
        return new ClubOperationResult { Succeeded = false, Message = message };
    }
}
