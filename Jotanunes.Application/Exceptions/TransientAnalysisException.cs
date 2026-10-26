namespace Jotanunes.Application.Exceptions;

public class TransientAnalysisException(string message, Exception? innerException = null) : Exception(message, innerException);
