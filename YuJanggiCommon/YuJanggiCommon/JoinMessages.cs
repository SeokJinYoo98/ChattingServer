namespace YuJanggiCommon;

public static class JoinRules
{
    public const int MaxPlayerNameLength = 20;
}

public sealed record JoinRequest(string PlayerName);

public sealed record JoinResponse(
    Guid PlayerId,
    string PlayerName
);
