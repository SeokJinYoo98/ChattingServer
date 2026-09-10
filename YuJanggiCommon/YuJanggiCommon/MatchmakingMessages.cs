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

public sealed record MatchmakingStartRequest()
{
    public bool SelectFormation { get; init; }
}

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
    public bool RequiresFormationSelection { get; init; }
}

public enum GameFormation { HEHE, EHEH, EHHE, HEEH }

public sealed record SelectFormationRequest(Guid GameId, GameFormation Formation);

public sealed record FormationSelectedResponse(Guid GameId, GameFormation Formation);
