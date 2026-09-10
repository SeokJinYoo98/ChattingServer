namespace YuJanggiCommon;

public enum GamePieceType
{
    King,
    Chariot,
    Cannon,
    Horse,
    Elephant,
    Guard,
    Soldier
}

public sealed record BoardPieceState(
    int PieceId,
    int X,
    int Z,
    PlayerSide Side,
    GamePieceType PieceType
);

public sealed record GameStartEvent(
    Guid GameId,
    PlayerSide Side,
    PlayerSide CurrentTurn,
    IReadOnlyList<BoardPieceState> Pieces
)
{
    public GameFormation ChoFormation { get; init; } = GameFormation.EHHE;
    public GameFormation HanFormation { get; init; } = GameFormation.EHHE;
}
