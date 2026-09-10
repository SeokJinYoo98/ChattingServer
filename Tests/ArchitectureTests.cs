using Microsoft.VisualStudio.TestTools.UnitTesting;
using YuJanggiCommon;
using YuJanggiServer.Controllers;
using YuJanggiServer.Game;
using YuJanggiServer.Models;
using YuJanggiServer.Transport;
using YuJanggiServer.Views;

namespace YuJanggiServer.Tests;

[TestClass]
public sealed class ArchitectureTests
{
    [TestMethod]
    public async Task JoinControllerCanRunWithoutTcpClient()
    {
        var connection = new RecordingConnection();
        var player = new PlayerSession(connection);
        var model = new ServerModel(new GameSessionFactory(), new Random(0));
        Assert.IsTrue(model.TryRegister(player));
        var controller = new JoinController(model, new ServerView());
        var message = ChatMessage.Create(
            MessageType.Join,
            "join-1",
            new JoinRequest("플레이어"));

        await controller.HandleAsync(player, message);

        Assert.AreEqual(1, connection.Sent.Count);
        ChatMessage response = connection.Sent[0];
        Assert.AreEqual(MessageType.Join, response.Type);
        Assert.AreEqual("join-1", response.RequestId);
        Assert.AreEqual("플레이어", response.GetPayload<JoinResponse>().PlayerName);
    }

    [TestMethod]
    public void GameSessionUsesInjectedEngineForFormationLifecycle()
    {
        Guid gameId = Guid.NewGuid();
        PlayerSession cho = CreateMatchedPlayer(gameId, PlayerSide.Cho, "초");
        PlayerSession han = CreateMatchedPlayer(gameId, PlayerSide.Han, "한");
        var engine = new RecordingGameEngine();
        var game = new GameSession(gameId, cho, han, engine, true, true);

        Assert.IsFalse(game.AnnounceMatch());
        Assert.IsNull(game.TrySelectFormation(
            cho,
            new SelectFormationRequest(gameId, GameFormation.HEHE),
            out bool choStarted));
        Assert.IsFalse(choStarted);
        Assert.IsNull(game.TrySelectFormation(
            han,
            new SelectFormationRequest(gameId, GameFormation.EHEH),
            out bool hanStarted));

        Assert.IsTrue(hanStarted);
        Assert.AreEqual(GameFormation.HEHE, engine.ChoFormation);
        Assert.AreEqual(GameFormation.EHEH, engine.HanFormation);
        Assert.AreEqual(1, engine.StartCount);
    }

    [TestMethod]
    public async Task DispatcherReturnsErrorForServerOnlyMessage()
    {
        var connection = new RecordingConnection();
        var player = new PlayerSession(connection);
        var dispatcher = new MessageDispatcher(
            Array.Empty<IMessageController>(),
            new ServerView());
        var message = ChatMessage.Create(
            MessageType.GameStart,
            "invalid-1",
            new { });

        await dispatcher.DispatchAsync(player, message);

        ErrorResponse error = connection.Sent.Single().GetPayload<ErrorResponse>();
        Assert.AreEqual(ErrorCode.UnsupportedMessageType, error.Code);
        Assert.AreEqual("invalid-1", connection.Sent.Single().RequestId);
    }

    private static PlayerSession CreateMatchedPlayer(
        Guid gameId,
        PlayerSide side,
        string name)
    {
        var player = new PlayerSession(new RecordingConnection());
        Assert.IsTrue(player.TryJoin(name, out _));
        player.SetMatch(gameId, side);
        return player;
    }

    private sealed class RecordingConnection : IClientConnection
    {
        public string ConnectionInfo => "test";
        public List<ChatMessage> Sent { get; } = new();

        public Task SendAsync(
            ChatMessage message,
            CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }

        public Task<ChatMessage> ReceiveAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class RecordingGameEngine : IJanggiGameEngine
    {
        public PlayerSide CurrentTurn => PlayerSide.Cho;
        public GameFormation? ChoFormation { get; private set; }
        public GameFormation? HanFormation { get; private set; }
        public int StartCount { get; private set; }

        public void Initialize(GameFormation choFormation, GameFormation hanFormation)
        {
            ChoFormation = choFormation;
            HanFormation = hanFormation;
        }

        public void Start() => StartCount++;

        public ErrorCode? TryGetLegalMoves(
            PlayerSide player,
            BoardPosition from,
            out LegalMovesResult? result)
        {
            result = null;
            return ErrorCode.NotImplemented;
        }

        public ErrorCode? TryMove(
            PlayerSide player,
            MoveRequest request,
            out EngineMoveResult? result)
        {
            result = null;
            return ErrorCode.NotImplemented;
        }

        public IReadOnlyList<BoardPieceState> CreateSnapshot()
        {
            return Array.Empty<BoardPieceState>();
        }

        public void Close()
        {
        }
    }
}
