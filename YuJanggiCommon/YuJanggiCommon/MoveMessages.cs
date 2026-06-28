namespace YuJanggiCommon;

public sealed record BoardPosition(
    int X,
    int Z
);

public sealed record LegalMovesRequest(
    BoardPosition From
);

public sealed record LegalMovesResult(
    BoardPosition From,
    IReadOnlyList<BoardPosition> LegalMoves
);

public sealed record MoveRequest(
    BoardPosition From,
    BoardPosition To
);

public sealed record MoveResultEvent(
    Guid GameId,
    BoardPosition From,
    BoardPosition To,
    PlayerSide MovedBy,
    PlayerSide CurrentTurn,
    IReadOnlyList<BoardPieceState> Pieces
);
