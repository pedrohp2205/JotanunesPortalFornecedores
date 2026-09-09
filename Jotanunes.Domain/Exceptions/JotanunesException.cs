namespace Jotanunes.Domain.Exceptions;

public class JotanunesException : Exception
{
    public JotanunesException(string error) : base(error) { }

    public static void When(bool hasError, string error)
    {
        if (hasError)
        {
            throw new JotanunesException(error);
        }
    }
}
