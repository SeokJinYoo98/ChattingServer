namespace YuJanggiCommon;

public sealed record JoinRequest(string PlayerName);

public sealed record JoinResponse(
    Guid PlayerId,
    string PlayerName
);
