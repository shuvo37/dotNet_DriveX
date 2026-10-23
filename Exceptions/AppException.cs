namespace DriveX.Exceptions;

// A business-rule failure with a message the frontend can show (e.g. "Invalid email or password").
// Becomes HTTP 400 with { "message": "..." }, like RuntimeException did in Spring.
public class AppException : Exception
{
    public AppException(string message) : base(message)
    {
    }
}
