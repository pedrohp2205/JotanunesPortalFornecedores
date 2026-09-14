namespace Jotanunes.Domain.Exceptions;

public class JotanunesException(string error) : Exception(error)
{
    public static void When(bool hasError, string error)
    {
        if (hasError)
        {
            throw new JotanunesException(error);
        }
    }
}
