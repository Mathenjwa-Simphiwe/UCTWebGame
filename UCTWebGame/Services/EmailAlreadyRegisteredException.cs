namespace UCTWebGame.Services;

public sealed class EmailAlreadyRegisteredException : Exception
{
    public EmailAlreadyRegisteredException()
        : base("An account with that email already exists.")
    {
    }
}
