namespace YuJanggiCommon;

public enum ErrorCode
{
    InvalidRequest,
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
