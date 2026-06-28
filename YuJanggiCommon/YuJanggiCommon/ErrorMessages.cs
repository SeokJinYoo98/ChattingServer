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
    UnsupportedMessageType,
    NotImplemented,
    PlayerNameRequired,
    PlayerNameTooLong,
    DuplicatePlayerName
}

public sealed record ErrorResponse(
    ErrorCode Code,
    string Message
);
