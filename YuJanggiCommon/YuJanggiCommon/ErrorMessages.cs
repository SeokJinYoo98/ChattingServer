namespace YuJanggiCommon;

public enum ErrorCode
{
    InvalidRequest,
    AlreadyJoined,
    NotJoined,
    AlreadyMatchmaking,
    NotMatchmaking,
    AlreadyMatched,
    NotMatched,
    GameSessionNotFound,
    ChatMessageRequired,
    ChatMessageTooLong,
    NotYourTurn,
    InvalidPosition,
    PieceNotFound,
    NotYourPiece,
    IllegalMove,
    UnsupportedMessageType,
    NotImplemented,
    PlayerNameRequired,
    PlayerNameTooLong,
    DuplicatePlayerName,
    GameNotStarted,
    InvalidFormation,
    FormationAlreadySelected,
    GameAlreadyStarted
}

public sealed record ErrorResponse(
    ErrorCode Code,
    string Message
);
