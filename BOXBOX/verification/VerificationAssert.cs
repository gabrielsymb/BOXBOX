namespace Boxbox.Verification;

internal static class VerificationAssert
{
    internal static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void Expect<TException>(Action action) where TException : Exception
    {
        _ = Capture<TException>(action);
    }

    internal static TException Capture<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"Expected exception {typeof(TException).Name} was not thrown.");
    }
}
