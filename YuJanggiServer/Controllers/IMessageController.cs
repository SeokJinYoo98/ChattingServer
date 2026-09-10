using YuJanggiCommon;
using YuJanggiServer.Models;

namespace YuJanggiServer.Controllers;

public interface IMessageController
{
    IReadOnlyCollection<MessageType> SupportedTypes { get; }

    Task HandleAsync(
        PlayerSession player,
        ChatMessage message,
        CancellationToken cancellationToken = default);
}
