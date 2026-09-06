namespace YuJanggiCommon;

public enum MatchmakingState
{
    Waiting,
    Cancelled
}

public enum PlayerSide
{
    Cho,
    Han
}

public sealed record MatchmakingStartRequest();

public sealed record MatchmakingCancelRequest();

public sealed record MatchmakingStatusResponse(
    MatchmakingState State
);

public sealed record MatchedPlayer(
    Guid PlayerId,
    string PlayerName
);

public sealed record MatchFoundResponse(
    Guid GameId,
    MatchedPlayer Opponent,
    PlayerSide Side
)
{
    public string Message { get; init; } = string.Empty;
}
