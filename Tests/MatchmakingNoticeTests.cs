using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YuJanggiCommon;

namespace YuJanggiServer.Tests;

[TestClass]
public sealed class MatchmakingNoticeTests
{
    [DataTestMethod]
    [DataRow("클라B", "클라A")]
    [DataRow("클라A", "클라B")]
    public async Task MatchedClientsReceiveSameTextWithActualSides(string choName, string hanName)
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        int port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        var server = new YuJanggiServer(port);
        Task running = server.StartAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        CancellationToken token = timeout.Token;
        using var choClient = new TcpClient();
        using var hanClient = new TcpClient();
        try
        {
            await choClient.ConnectAsync(IPAddress.Loopback, port, token);
            await hanClient.ConnectAsync(IPAddress.Loopback, port, token);
            NetworkStream cho = choClient.GetStream();
            NetworkStream han = hanClient.GetStream();
            await Send(cho, MessageType.Join, new JoinRequest(choName), token);
            await Receive<JoinResponse>(cho, MessageType.Join, token);
            await Send(han, MessageType.Join, new JoinRequest(hanName), token);
            await Receive<JoinResponse>(han, MessageType.Join, token);

            await Send(cho, MessageType.MatchmakingStart, new MatchmakingStartRequest(), token);
            var waiting = await Receive<MatchmakingStatusResponse>(cho, MessageType.MatchmakingStatus, token);
            Assert.AreEqual(MatchmakingState.Waiting, waiting.State);
            // 대기를 취소한 연결도 다시 매칭에 참가할 수 있어야 합니다.
            await Send(cho, MessageType.MatchmakingCancel, new MatchmakingCancelRequest(), token);
            var cancelled = await Receive<MatchmakingStatusResponse>(cho, MessageType.MatchmakingStatus, token);
            Assert.AreEqual(MatchmakingState.Cancelled, cancelled.State);
            await Send(cho, MessageType.MatchmakingStart, new MatchmakingStartRequest(), token);
            await Receive<MatchmakingStatusResponse>(cho, MessageType.MatchmakingStatus, token);

            await Send(han, MessageType.MatchmakingStart, new MatchmakingStartRequest(), token);
            var choMatch = await Receive<MatchFoundResponse>(cho, MessageType.MatchFound, token);
            var hanMatch = await Receive<MatchFoundResponse>(han, MessageType.MatchFound, token);
            Assert.AreEqual($"초: {choName}\n한: {hanName}", choMatch.Message);
            Assert.AreEqual(choMatch.Message, hanMatch.Message);
            Assert.AreEqual(choMatch.GameId, hanMatch.GameId);
            Assert.AreEqual(PlayerSide.Cho, choMatch.Side);
            Assert.AreEqual(PlayerSide.Han, hanMatch.Side);
            Assert.AreEqual(hanName, choMatch.Opponent.PlayerName);
            Assert.AreEqual(choName, hanMatch.Opponent.PlayerName);
            var choStart = await Receive<GameStartEvent>(cho, MessageType.GameStart, token);
            var hanStart = await Receive<GameStartEvent>(han, MessageType.GameStart, token);
            Assert.AreEqual(choMatch.GameId, choStart.GameId);
            Assert.AreEqual(hanMatch.GameId, hanStart.GameId);
            Assert.AreEqual(choMatch.Side, choStart.Side);
            Assert.AreEqual(hanMatch.Side, hanStart.Side);
        }
        finally
        {
            server.Stop();
            await running.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestMethod]
    public void NewClientCanReadMatchFromOldServerWithoutText()
    {
        var legacy = new LegacyMatchFound(Guid.NewGuid(), new MatchedPlayer(Guid.NewGuid(), "클라A"), PlayerSide.Han);
        var message = ChatMessage.Create(MessageType.MatchFound, null, legacy);
        var match = message.GetPayload<MatchFoundResponse>();
        Assert.AreEqual(string.Empty, match.Message);
        Assert.AreEqual(legacy.GameId, match.GameId);
        Assert.AreEqual(legacy.Side, match.Side);
    }

    [TestMethod]
    public void OldClientCanReadMatchWithAdditionalText()
    {
        var match = new MatchFoundResponse(Guid.NewGuid(), new MatchedPlayer(Guid.NewGuid(), "클라A"), PlayerSide.Cho)
        {
            Message = "초: 클라B\n한: 클라A"
        };
        var legacy = ChatMessage.Create(MessageType.MatchFound, null, match).GetPayload<LegacyMatchFound>();
        Assert.AreEqual(match.GameId, legacy.GameId);
        Assert.AreEqual(match.Side, legacy.Side);
        Assert.AreEqual(match.Opponent, legacy.Opponent);
    }

    private static async Task Send<T>(NetworkStream stream, MessageType type, T payload, CancellationToken token)
    {
        byte[] packet = MessageProtocol.Encode(ChatMessage.Create(type, Guid.NewGuid().ToString("N"), payload));
        await stream.WriteAsync(packet, token);
    }

    private static async Task<T> Receive<T>(NetworkStream stream, MessageType type, CancellationToken token)
    {
        byte[] header = new byte[MessageProtocol.HeaderSize];
        await stream.ReadExactlyAsync(header, token);
        byte[] body = new byte[MessageProtocol.DecodeBodyLength(header)];
        await stream.ReadExactlyAsync(body, token);
        ChatMessage message = MessageProtocol.DecodeBody(body);
        Assert.AreEqual(type, message.Type);
        return message.GetPayload<T>();
    }

    public sealed record LegacyMatchFound(Guid GameId, MatchedPlayer Opponent, PlayerSide Side);
}
