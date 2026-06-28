namespace YuJanggiCommon;

public static class GameChatRules
{
    public const int MaxMessageLength = 200;
}

public sealed record GameChatSendRequest(
    string Message
);

public sealed record GameChatReceivedEvent(
    Guid GameId,
    Guid SenderPlayerId,
    string SenderPlayerName,
    string Message,
    DateTimeOffset SentAt
);
