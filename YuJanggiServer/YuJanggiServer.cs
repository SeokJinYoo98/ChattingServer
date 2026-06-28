using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using YuJanggiServer.Client;
using YuJanggiServer.Game;
using YuJanggiCommon;

namespace YuJanggiServer;

public class YuJanggiServer
{
    private readonly List<ClientSession> _clients = new();
    private readonly List<MatchmakingEntry> _matchmakingQueue = new();
    private readonly Dictionary<Guid, GameSession> _gameSessions = new();
    private readonly Lock _clientsLock = new();
    private readonly TcpListener _listener;

    private bool _isRunning;
    private bool _isClearingClients;

    public YuJanggiServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        _isRunning = true;

        try
        {
            while (_isRunning)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                ClientSession session = new(client);
                bool accepted;

                lock (_clientsLock)
                {
                    accepted = !_isClearingClients;

                    if (accepted)
                    {
                        _clients.Add(session);
                    }
                }

                if (!accepted)
                {
                    session.Dispose();
                    continue;
                }

                Console.WriteLine($"[Connect] {session.ClientInfo}");
                _ = HandleClientAsync(session);
            }
        }
        catch (SocketException) when (!_isRunning)
        {
            // Stop() 호출로 접속 대기가 종료된 경우
        }
        catch (ObjectDisposedException) when (!_isRunning)
        {
            // Stop() 호출로 리스너가 정리된 경우
        }
    }

    public void PrintClientList()
    {
        List<string> clients;

        lock (_clientsLock)
        {
            clients = _clients
                .Select(session => session.ClientInfo)
                .ToList();
        }

        Console.WriteLine($"[ClientList] 접속 인원: {clients.Count}");

        foreach (string client in clients)
        {
            Console.WriteLine(client);
        }
    }

    public void ClearClients()
    {
        List<ClientSession> clients;

        lock (_clientsLock)
        {
            _isClearingClients = true;
            clients = _clients.ToList();
            _clients.Clear();
            _matchmakingQueue.Clear();
            _gameSessions.Clear();
        }

        try
        {
            foreach (ClientSession session in clients)
            {
                session.Dispose();
            }

            Console.WriteLine(
                $"[ClientClear] 클라이언트 {clients.Count}명의 연결을 종료했습니다."
            );
        }
        finally
        {
            lock (_clientsLock)
            {
                _isClearingClients = false;
            }
        }
    }

    private async Task HandleClientAsync(ClientSession session)
    {
        try
        {
            while (true)
            {
                ChatMessage message = await session.ReceiveAsync();

                Console.WriteLine(
                    $"[Receive] {session.ClientInfo} | Type={message.Type}"
                );

                await DispatchMessageAsync(session, message);
            }
        }
        catch (IOException)
        {
            // 연결 종료 또는 통신 오류
        }
        catch (ObjectDisposedException)
        {
            // 서버 종료 과정에서 세션이 정리된 경우
        }
        finally
        {
            ClientSession? opponent = null;
            Guid? endedGameId = null;

            lock (_clientsLock)
            {
                _clients.Remove(session);
                _matchmakingQueue.RemoveAll(entry =>
                    ReferenceEquals(entry.Session, session)
                );

                if (session.GameId is Guid activeGameId &&
                    _gameSessions.Remove(
                        activeGameId,
                        out GameSession? gameSession
                    ))
                {
                    opponent = gameSession.GetOpponent(session);
                    endedGameId = activeGameId;
                    gameSession.ClearPlayers();
                }
            }

            session.Dispose();
            Console.WriteLine($"[Disconnect] {session.ClientInfo}");

            if (opponent is not null &&
                endedGameId is Guid closedGameId)
            {
                try
                {
                    await opponent.SendAsync(ChatMessage.Create(
                        MessageType.GameEnd,
                        null,
                        new GameEndEvent(
                            closedGameId,
                            GameEndReason.OpponentLeft,
                            "상대 플레이어가 채팅방을 나갔습니다."
                        )
                    ));
                }
                catch (Exception exception) when (
                    exception is IOException or ObjectDisposedException)
                {
                    // 남은 클라이언트도 이미 연결을 종료한 경우
                }
            }
        }
    }

    private Task DispatchMessageAsync(
        ClientSession session,
        ChatMessage message)
    {
        return message.Type switch
        {
            MessageType.Join => HandleJoinAsync(session, message),
            MessageType.MatchmakingStart =>
                HandleMatchmakingStartAsync(session, message),
            MessageType.MatchmakingCancel =>
                HandleMatchmakingCancelAsync(session, message),
            MessageType.GameChatSend =>
                HandleGameChatAsync(session, message),
            MessageType.LegalMovesRequest =>
                HandleLegalMovesAsync(session, message),
            MessageType.MoveRequest =>
                HandleMoveAsync(session, message),
            MessageType.MatchmakingStatus or
            MessageType.MatchFound or
            MessageType.GameChatReceived or
            MessageType.GameStart or
            MessageType.LegalMovesResult or
            MessageType.MoveResult or
            MessageType.TurnChanged or
            MessageType.GameEnd or
            MessageType.Error => SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.UnsupportedMessageType,
                $"클라이언트가 보낼 수 없는 메시지 타입입니다: {message.Type}"
            ),
            _ => SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.UnsupportedMessageType,
                $"지원하지 않는 메시지 타입입니다: {message.Type}"
            )
        };
    }

    private Task HandleJoinAsync(
        ClientSession session,
        ChatMessage message)
    {
        JoinRequest request;

        try
        {
            request = message.GetPayload<JoinRequest>();
        }
        catch (Exception exception) when (
            exception is InvalidDataException or JsonException)
        {
            return SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.InvalidRequest,
                "Join Payload 형식이 올바르지 않습니다."
            );
        }

        string playerName = request.PlayerName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(playerName))
        {
            return SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.PlayerNameRequired,
                "플레이어 이름은 필수입니다."
            );
        }

        if (playerName.Length > JoinRules.MaxPlayerNameLength)
        {
            return SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.PlayerNameTooLong,
                $"플레이어 이름은 {JoinRules.MaxPlayerNameLength}자 이하여야 합니다."
            );
        }

        ErrorCode? joinError = null;
        Guid playerId = default;

        lock (_clientsLock)
        {
            if (session.IsJoined)
            {
                joinError = ErrorCode.AlreadyJoined;
            }
            else if (_clients.Any(other =>
                !ReferenceEquals(other, session) &&
                string.Equals(
                    other.PlayerName,
                    playerName,
                    StringComparison.OrdinalIgnoreCase
                )))
            {
                joinError = ErrorCode.DuplicatePlayerName;
            }
            else if (!session.TryJoin(playerName, out playerId))
            {
                joinError = ErrorCode.AlreadyJoined;
            }
        }

        if (joinError is ErrorCode errorCode)
        {
            string errorMessage = errorCode == ErrorCode.DuplicatePlayerName
                ? "이미 사용 중인 플레이어 이름입니다."
                : "이미 참가한 세션입니다.";

            return SendErrorAsync(
                session,
                message.RequestId,
                errorCode,
                errorMessage
            );
        }

        return session.SendAsync(ChatMessage.Create(
            MessageType.Join,
            message.RequestId,
            new JoinResponse(playerId, playerName)
        ));
    }

    private async Task HandleMatchmakingStartAsync(
        ClientSession session,
        ChatMessage message)
    {
        try
        {
            _ = message.GetPayload<MatchmakingStartRequest>();
        }
        catch (Exception exception) when (
            exception is InvalidDataException or JsonException)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.InvalidRequest,
                "MatchmakingStart Payload 형식이 올바르지 않습니다."
            );
            return;
        }

        ErrorCode? matchmakingError = null;
        MatchmakingEntry? first = null;
        MatchmakingEntry? second = null;
        GameSession? createdGame = null;
        Guid gameId = default;

        lock (_clientsLock)
        {
            if (!session.IsJoined)
            {
                matchmakingError = ErrorCode.NotJoined;
            }
            else if (session.IsMatched)
            {
                matchmakingError = ErrorCode.AlreadyMatched;
            }
            else if (_matchmakingQueue.Any(entry =>
                ReferenceEquals(entry.Session, session)))
            {
                matchmakingError = ErrorCode.AlreadyMatchmaking;
            }
            else
            {
                _matchmakingQueue.Add(new MatchmakingEntry(
                    session,
                    message.RequestId
                ));

                if (_matchmakingQueue.Count >= 2)
                {
                    first = _matchmakingQueue[0];
                    second = _matchmakingQueue[1];
                    _matchmakingQueue.RemoveRange(0, 2);

                    gameId = Guid.NewGuid();
                    first.Session.SetMatch(gameId, PlayerSide.Cho);
                    second.Session.SetMatch(gameId, PlayerSide.Han);
                    createdGame = new GameSession(
                        gameId,
                        first.Session,
                        second.Session
                    );
                    _gameSessions.Add(gameId, createdGame);
                }
            }
        }

        if (matchmakingError is ErrorCode errorCode)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                errorCode,
                GetMatchmakingErrorMessage(errorCode)
            );
            return;
        }

        if (first is null || second is null)
        {
            await session.SendAsync(ChatMessage.Create(
                MessageType.MatchmakingStatus,
                message.RequestId,
                new MatchmakingStatusResponse(MatchmakingState.Waiting)
            ));
            return;
        }

        MatchedPlayer firstPlayer = CreateMatchedPlayer(first.Session);
        MatchedPlayer secondPlayer = CreateMatchedPlayer(second.Session);

        await Task.WhenAll(
            first.Session.SendAsync(ChatMessage.Create(
                MessageType.MatchFound,
                first.RequestId,
                new MatchFoundResponse(
                    gameId,
                    secondPlayer,
                    PlayerSide.Cho
                )
            )),
            second.Session.SendAsync(ChatMessage.Create(
                MessageType.MatchFound,
                second.RequestId,
                new MatchFoundResponse(
                    gameId,
                    firstPlayer,
                    PlayerSide.Han
                )
            ))
        );

        if (createdGame is null)
        {
            throw new InvalidOperationException(
                "매칭된 게임 세션이 생성되지 않았습니다."
            );
        }

        await Task.WhenAll(
            first.Session.SendAsync(ChatMessage.Create(
                MessageType.GameStart,
                null,
                createdGame.CreateGameStart(PlayerSide.Cho)
            )),
            second.Session.SendAsync(ChatMessage.Create(
                MessageType.GameStart,
                null,
                createdGame.CreateGameStart(PlayerSide.Han)
            ))
        );
    }

    private Task HandleMatchmakingCancelAsync(
        ClientSession session,
        ChatMessage message)
    {
        try
        {
            _ = message.GetPayload<MatchmakingCancelRequest>();
        }
        catch (Exception exception) when (
            exception is InvalidDataException or JsonException)
        {
            return SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.InvalidRequest,
                "MatchmakingCancel Payload 형식이 올바르지 않습니다."
            );
        }

        bool removed;

        lock (_clientsLock)
        {
            int index = _matchmakingQueue.FindIndex(entry =>
                ReferenceEquals(entry.Session, session)
            );

            removed = index >= 0;

            if (removed)
            {
                _matchmakingQueue.RemoveAt(index);
            }
        }

        if (!removed)
        {
            return SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.NotMatchmaking,
                GetMatchmakingErrorMessage(ErrorCode.NotMatchmaking)
            );
        }

        return session.SendAsync(ChatMessage.Create(
            MessageType.MatchmakingStatus,
            message.RequestId,
            new MatchmakingStatusResponse(MatchmakingState.Cancelled)
        ));
    }

    private async Task HandleLegalMovesAsync(
        ClientSession session,
        ChatMessage message)
    {
        LegalMovesRequest request;

        try
        {
            request = message.GetPayload<LegalMovesRequest>();
        }
        catch (Exception exception) when (
            exception is InvalidDataException or JsonException)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.InvalidRequest,
                "LegalMovesRequest Payload 형식이 올바르지 않습니다."
            );
            return;
        }

        if (request.From is null)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.InvalidRequest,
                "선택 좌표가 필요합니다."
            );
            return;
        }

        ErrorCode? sessionError =
            TryGetGameSession(session, out GameSession? gameSession);

        if (sessionError is ErrorCode gameError)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                gameError,
                GetMoveErrorMessage(gameError)
            );
            return;
        }

        if (gameSession is null)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.GameSessionNotFound,
                GetMoveErrorMessage(ErrorCode.GameSessionNotFound)
            );
            return;
        }

        ErrorCode? moveError = gameSession.TryGetLegalMoves(
            session,
            request.From,
            out LegalMovesResult? result
        );

        if (moveError is ErrorCode errorCode || result is null)
        {
            ErrorCode responseCode =
                moveError ?? ErrorCode.InvalidRequest;

            await SendErrorAsync(
                session,
                message.RequestId,
                responseCode,
                GetMoveErrorMessage(responseCode)
            );
            return;
        }

        await session.SendAsync(ChatMessage.Create(
            MessageType.LegalMovesResult,
            message.RequestId,
            result
        ));
    }

    private async Task HandleMoveAsync(
        ClientSession session,
        ChatMessage message)
    {
        MoveRequest request;

        try
        {
            request = message.GetPayload<MoveRequest>();
        }
        catch (Exception exception) when (
            exception is InvalidDataException or JsonException)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.InvalidRequest,
                "MoveRequest Payload 형식이 올바르지 않습니다."
            );
            return;
        }

        if (request.From is null || request.To is null)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.InvalidRequest,
                "시작 좌표와 도착 좌표가 필요합니다."
            );
            return;
        }

        ErrorCode? sessionError =
            TryGetGameSession(session, out GameSession? gameSession);

        if (sessionError is ErrorCode gameError)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                gameError,
                GetMoveErrorMessage(gameError)
            );
            return;
        }

        if (gameSession is null)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.GameSessionNotFound,
                GetMoveErrorMessage(ErrorCode.GameSessionNotFound)
            );
            return;
        }

        ErrorCode? moveError = gameSession.TryMove(
            session,
            request,
            out MoveResultEvent? result
        );

        if (moveError is ErrorCode errorCode || result is null)
        {
            ErrorCode responseCode =
                moveError ?? ErrorCode.InvalidRequest;

            await SendErrorAsync(
                session,
                message.RequestId,
                responseCode,
                GetMoveErrorMessage(responseCode)
            );
            return;
        }

        ClientSession opponent = gameSession.GetOpponent(session);

        await Task.WhenAll(
            session.SendAsync(ChatMessage.Create(
                MessageType.MoveResult,
                message.RequestId,
                result
            )),
            opponent.SendAsync(ChatMessage.Create(
                MessageType.MoveResult,
                null,
                result
            ))
        );
    }

    private ErrorCode? TryGetGameSession(
        ClientSession session,
        out GameSession? gameSession)
    {
        lock (_clientsLock)
        {
            if (session.GameId is not Guid gameId)
            {
                gameSession = null;
                return ErrorCode.NotMatched;
            }

            if (!_gameSessions.TryGetValue(gameId, out gameSession) ||
                !gameSession.Contains(session))
            {
                gameSession = null;
                return ErrorCode.GameSessionNotFound;
            }

            return null;
        }
    }

    private static string GetMoveErrorMessage(ErrorCode errorCode)
    {
        return errorCode switch
        {
            ErrorCode.NotMatched =>
                "매칭 완료 후 기물을 선택할 수 있습니다.",
            ErrorCode.GameSessionNotFound =>
                "게임 세션을 찾을 수 없습니다.",
            ErrorCode.NotYourTurn => "현재 플레이어의 턴이 아닙니다.",
            ErrorCode.InvalidPosition => "보드 좌표가 올바르지 않습니다.",
            ErrorCode.PieceNotFound => "선택한 위치에 기물이 없습니다.",
            ErrorCode.NotYourPiece => "상대 기물은 선택할 수 없습니다.",
            ErrorCode.IllegalMove => "이동할 수 없는 위치입니다.",
            _ => "이동 요청을 처리할 수 없습니다."
        };
    }
    private async Task HandleGameChatAsync(
        ClientSession session,
        ChatMessage message)
    {
        GameChatSendRequest request;

        try
        {
            request = message.GetPayload<GameChatSendRequest>();
        }
        catch (Exception exception) when (
            exception is InvalidDataException or JsonException)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.InvalidRequest,
                "GameChatSend Payload 형식이 올바르지 않습니다."
            );
            return;
        }

        string chatMessage = request.Message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(chatMessage))
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.ChatMessageRequired,
                "채팅 메시지는 필수입니다."
            );
            return;
        }

        if (chatMessage.Length > GameChatRules.MaxMessageLength)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.ChatMessageTooLong,
                $"채팅 메시지는 {GameChatRules.MaxMessageLength}자 이하여야 합니다."
            );
            return;
        }

        GameSession? gameSession;
        ErrorCode? chatError = null;

        lock (_clientsLock)
        {
            if (session.GameId is not Guid gameId)
            {
                chatError = ErrorCode.NotMatched;
                gameSession = null;
            }
            else if (!_gameSessions.TryGetValue(
                gameId,
                out gameSession
            ) || !gameSession.Contains(session))
            {
                chatError = ErrorCode.GameSessionNotFound;
                gameSession = null;
            }
        }

        if (chatError is ErrorCode errorCode)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                errorCode,
                errorCode == ErrorCode.NotMatched
                    ? "매칭 완료 후 채팅을 보낼 수 있습니다."
                    : "게임 세션을 찾을 수 없습니다."
            );
            return;
        }

        if (gameSession is null)
        {
            await SendErrorAsync(
                session,
                message.RequestId,
                ErrorCode.GameSessionNotFound,
                "게임 세션을 찾을 수 없습니다."
            );
            return;
        }

        MatchedPlayer sender = CreateMatchedPlayer(session);
        ClientSession opponent = gameSession.GetOpponent(session);
        GameChatReceivedEvent chatEvent = new(
            gameSession.GameId,
            sender.PlayerId,
            sender.PlayerName,
            chatMessage,
            DateTimeOffset.UtcNow
        );

        await Task.WhenAll(
            session.SendAsync(ChatMessage.Create(
                MessageType.GameChatReceived,
                message.RequestId,
                chatEvent
            )),
            opponent.SendAsync(ChatMessage.Create(
                MessageType.GameChatReceived,
                null,
                chatEvent
            ))
        );
    }

    private static MatchedPlayer CreateMatchedPlayer(ClientSession session)
    {
        if (session.PlayerId is not Guid playerId ||
            session.PlayerName is not string playerName)
        {
            throw new InvalidOperationException(
                "참가하지 않은 세션은 매칭될 수 없습니다."
            );
        }

        return new MatchedPlayer(playerId, playerName);
    }

    private static string GetMatchmakingErrorMessage(ErrorCode errorCode)
    {
        return errorCode switch
        {
            ErrorCode.NotJoined => "참가 완료 후 매칭을 시작할 수 있습니다.",
            ErrorCode.AlreadyMatchmaking => "이미 매칭 대기 중입니다.",
            ErrorCode.NotMatchmaking => "매칭 대기 중이 아닙니다.",
            ErrorCode.AlreadyMatched => "이미 매칭이 완료된 세션입니다.",
            _ => "매칭 요청을 처리할 수 없습니다."
        };
    }

    private static Task SendErrorAsync(
        ClientSession session,
        string? requestId,
        ErrorCode code,
        string message)
    {
        return session.SendAsync(ChatMessage.Create(
            MessageType.Error,
            requestId,
            new ErrorResponse(code, message)
        ));
    }

    private sealed record MatchmakingEntry(
        ClientSession Session,
        string? RequestId
    );

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        ClearClients();
    }
}
