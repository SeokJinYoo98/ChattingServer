using System.Net.Sockets;
using YuJanggiCommon;

namespace YuJanggiServer.Client;

public sealed class ClientSession : IDisposable
{
    public TcpClient Client { get; }
    public NetworkStream Stream { get; }
    public string ClientInfo { get; }
    public Guid? PlayerId { get; private set; }
    public string? PlayerName { get; private set; }
    public bool IsJoined => PlayerId.HasValue;
    public Guid? GameId { get; private set; }
    public PlayerSide? Side { get; private set; }
    public bool IsMatched => GameId.HasValue;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public ClientSession(TcpClient client)
    {
        Client = client;
        Stream = client.GetStream();
        ClientInfo = client.Client.RemoteEndPoint?.ToString()
            ?? "Unknown";
    }

    public bool TryJoin(string playerName, out Guid playerId)
    {
        if (IsJoined)
        {
            playerId = default;
            return false;
        }

        playerId = Guid.NewGuid();
        PlayerId = playerId;
        PlayerName = playerName;
        return true;
    }

    public void SetMatch(Guid gameId, PlayerSide side)
    {
        GameId = gameId;
        Side = side;
    }

    public void ClearMatch()
    {
        GameId = null;
        Side = null;
    }

    public async Task SendAsync(ChatMessage message)
    {
        byte[] packet = MessageProtocol.Encode(message);

        await _sendLock.WaitAsync();

        try
        {
            await Stream.WriteAsync(packet);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<ChatMessage> ReceiveAsync()
    {
        byte[] header = new byte[MessageProtocol.HeaderSize];

        await Stream.ReadExactlyAsync(header);

        int bodyLength = MessageProtocol.DecodeBodyLength(header);
        byte[] body = new byte[bodyLength];

        await Stream.ReadExactlyAsync(body);

        return MessageProtocol.DecodeBody(body);
    }

    public void Dispose()
    {
        Stream.Dispose();
        Client.Dispose();
        _sendLock.Dispose();
    }
}
