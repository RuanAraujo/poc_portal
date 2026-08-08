namespace Documentation.Ingestion.Application.Exceptions;

public sealed class PermanentIngestionException : Exception
{
    public PermanentIngestionException(string message)
        : base(message)
    {
    }

    public PermanentIngestionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
