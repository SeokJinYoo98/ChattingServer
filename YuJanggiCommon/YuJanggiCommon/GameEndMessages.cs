namespace YuJanggiCommon;

public enum GameEndReason
{
    OpponentLeft
}

public sealed record GameEndEvent(
    Guid GameId,
    GameEndReason Reason,
    string Message
);
