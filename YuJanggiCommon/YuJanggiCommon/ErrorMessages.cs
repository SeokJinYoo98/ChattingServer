namespace YuJanggiCommon;

public enum ErrorCode
{
    InvalidRequest,
    AlreadyJoined,
    NotJoined,
    AlreadyMatchmaking,
    NotMatchmaking,
    AlreadyMatched,
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
