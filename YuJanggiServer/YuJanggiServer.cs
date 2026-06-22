
using System.Net;
using System.Net.Sockets;
using MyServer.Client;
using YuJanggiCommon;
namespace MyServer;
public class YuJanggiServer
{
    private readonly List<ClientSession> _clients       = new();
    private readonly Lock                _clientsLock   = new();
    private readonly TcpListener         _listener;
    private readonly UserAccountStore    _accountStore;

    private bool _isRunning;
    private bool _isClearingPlayers;

    public YuJanggiServer(int port)
    {
        _listener = new TcpListener(
            IPAddress.Any,
            port
        );

        _accountStore = new UserAccountStore(
            Path.Combine(Directory.GetCurrentDirectory(), "UserData")
        );
    }

    // 클라이언트 접속을 비동기로 기다린다.
    // 대기 중에는 스레드를 점유하지 않는다.
    public async Task StartAsync()
    {
        _listener.Start();
        _isRunning = true;

        try
        {
            while (_isRunning)
            {
                TcpClient client =
                    await _listener.AcceptTcpClientAsync();

                ClientSession session =
                    new ClientSession(client);

                bool accepted;

                lock (_clientsLock)
                {
                    accepted = !_isClearingPlayers;

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

                Console.WriteLine(
                    $"[Connect] {session.ClientInfo}"
                );

                // 클라이언트 처리를 시작하되,
                // 종료될 때까지 기다리지 않고 다음 접속을 받는다.
                _ = HandleClientAsync(session);
            }
        }
        catch (SocketException)
        {
            // Stop() 호출로 Accept 대기가 풀린 경우
        }
    }

    public void PrintPlayerList()
    {
        List<(string UserId, string Nickname)> players;

        lock (_clientsLock)
        {
            players = _clients
                .Where(session => session.IsAuthenticated)
                .Select(session => (
                    session.UserId ?? string.Empty,
                    session.Nickname ?? string.Empty
                ))
                .ToList();
        }

        Console.WriteLine($"[PlayerList] 접속 인원: {players.Count}");

        foreach ((string userId, string nickname) in players)
        {
            Console.WriteLine(
                $"아이디: {userId}, 닉네임: {nickname}"
            );
        }
    }

    public async Task ClearPlayersAsync()
    {
        List<ClientSession> clients;

        lock (_clientsLock)
        {
            _isClearingPlayers = true;
            clients = _clients.ToList();
            _clients.Clear();
        }

        try
        {
            foreach (ClientSession session in clients)
            {
                try
                {
                    await session.SendAsync(new ChatMessage
                    {
                        Type = MessageType.System,
                        Sender = "Server",
                        Content = "서버에 의해 접속이 종료되었습니다."
                    });
                }
                catch (IOException)
                {
                    // 이미 연결이 종료된 세션은 메시지를 전송할 수 없다.
                }
                catch (ObjectDisposedException)
                {
                    // 이미 정리된 세션은 메시지를 전송할 수 없다.
                }
                finally
                {
                    session.Dispose();
                }
            }

            await _accountStore.ClearAsync();

            Console.WriteLine(
                $"[PlayerClear] 계정 정보를 삭제하고 " +
                $"클라이언트 {clients.Count}명의 연결을 종료했습니다."
            );
        }
        finally
        {
            lock (_clientsLock)
            {
                _isClearingPlayers = false;
            }
        }
    }

    private async Task HandleClientAsync(
        ClientSession session)
    {
        try
        {
            while(true)
            {
                ChatMessage message = await session.ReceiveAsync();

                Console.WriteLine(
                    $"[Receive] {session.ClientInfo} | " +
                    $"Type={message.Type}, " +
                    $"Sender=\"{message.Sender}\", " +
                    $"Length={message.Content.Length}, " +
                    $"Content=\"{message.Content}\""
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
            string? nickname = session.Nickname;

            lock (_clientsLock)
            {
                _clients.Remove(session);
            }

            session.Dispose();

            if (!string.IsNullOrWhiteSpace(nickname))
            {
                await BroadcastAsync(new ChatMessage
                {
                    Type = MessageType.System,
                    Sender = "Server",
                    Content = $"{nickname}님이 서버를 떠나셨습니다."
                });
            }

            Console.WriteLine(
                $"[Disconnect] {session.ClientInfo}"
            );
        }
    }

    private async Task DispatchMessageAsync(
        ClientSession session,
        ChatMessage message)
    {
        if (!session.IsAuthenticated &&
            message.Type != MessageType.Register &&
            message.Type != MessageType.Login &&
            message.Type != MessageType.SetNickname &&
            message.Type != MessageType.CancelLogin)
        {
            await SendErrorAsync(
                session,
                "로그인과 닉네임 설정을 먼저 완료하세요."
            );
            return;
        }

        switch (message.Type)
        {
            case MessageType.Chat:
                await HandleChatAsync(session, message);
                break;
            case MessageType.System:
                await HandleSystemAsync(message);
                break;
            case MessageType.Register:
                await HandleRegisterAsync(session, message);
                break;
            case MessageType.Login:
                await HandleLoginAsync(session, message);
                break;
            case MessageType.SetNickname:
                await HandleSetNicknameAsync(session, message);
                break;
            case MessageType.CancelLogin:
                await HandleCancelLoginAsync(session);
                break;
            case MessageType.Leave:
                await HandleLeaveAsync(message);
                break;
            case MessageType.CreateRoom:
                await HandleCreateRoomAsync(message);
                break;
            case MessageType.JoinRoom:
                await HandleJoinRoomAsync(message);
                break;
            case MessageType.GameReady:
                await HandleGameReadyAsync(message);
                break;
            case MessageType.MovePiece:
                await HandleMovePieceAsync(message);
                break;
            case MessageType.Error:
                await HandleErrorAsync(message);
                break;
            default:
                await session.SendAsync(new ChatMessage
                {
                    Type = MessageType.Error,
                    Sender = "Server",
                    Content = $"지원하지 않는 메시지 타입입니다: {message.Type}"
                });
                break;
        }
    }

    private Task HandleChatAsync(
        ClientSession session,
        ChatMessage message)
    {
        message.Sender = session.Nickname ?? string.Empty;
        return BroadcastAsync(message);
    }

    private Task HandleSystemAsync(ChatMessage message) =>
        BroadcastAsync(message);

    private async Task HandleRegisterAsync(
        ClientSession session,
        ChatMessage message)
    {
        if (session.IsLoginVerified)
        {
            await SendErrorAsync(
                session,
                "로그인 후에는 회원가입할 수 없습니다."
            );
            return;
        }

        UserAccount? account = message.Account;

        if (account == null ||
            string.IsNullOrWhiteSpace(account.UserId) ||
            string.IsNullOrWhiteSpace(account.Password))
        {
            await SendRegisterResultAsync(
                session,
                false,
                "아이디와 비밀번호를 모두 입력하세요."
            );
            return;
        }

        account.UserId = account.UserId.Trim();

        try
        {
            bool registered = await _accountStore.RegisterAsync(account);

            await SendRegisterResultAsync(
                session,
                registered,
                registered
                    ? "회원가입이 완료되었습니다."
                    : "이미 사용 중인 아이디입니다."
            );
        }
        catch (IOException exception)
        {
            Console.WriteLine($"[Register Error] {exception.Message}");

            await SendRegisterResultAsync(
                session,
                false,
                "회원가입 정보를 저장하지 못했습니다."
            );
        }
    }

    private static Task SendRegisterResultAsync(
        ClientSession session,
        bool succeeded,
        string content) =>
        session.SendAsync(new ChatMessage
        {
            Type = succeeded ? MessageType.System : MessageType.Error,
            Sender = "Server",
            Content = content
        });

    private async Task HandleLoginAsync(
        ClientSession session,
        ChatMessage message)
    {
        if (session.IsLoginVerified)
        {
            await SendErrorAsync(
                session,
                "이미 로그인되었습니다."
            );
            return;
        }

        UserAccount? account = message.Account;

        if (account == null ||
            string.IsNullOrWhiteSpace(account.UserId) ||
            string.IsNullOrWhiteSpace(account.Password))
        {
            await SendErrorAsync(
                session,
                "아이디와 비밀번호를 모두 입력하세요."
            );
            return;
        }

        account.UserId = account.UserId.Trim();

        try
        {
            bool authenticated =
                await _accountStore.AuthenticateAsync(account);

            if (!authenticated)
            {
                await SendErrorAsync(
                    session,
                    "아이디 또는 비밀번호가 일치하지 않습니다."
                );
                return;
            }

            session.VerifyLogin(account.UserId);

            await session.SendAsync(new ChatMessage
            {
                Type = MessageType.LoginResult,
                Sender = "Server",
                Content = "로그인되었습니다. 닉네임을 설정하세요."
            });
        }
        catch (IOException exception)
        {
            Console.WriteLine($"[Login Error] {exception.Message}");

            await SendErrorAsync(
                session,
                "회원 정보를 확인하지 못했습니다."
            );
        }
    }

    private async Task HandleSetNicknameAsync(
        ClientSession session,
        ChatMessage message)
    {
        if (!session.IsLoginVerified)
        {
            await SendErrorAsync(
                session,
                "로그인을 먼저 완료하세요."
            );
            return;
        }

        if (session.IsAuthenticated)
        {
            await SendErrorAsync(
                session,
                "닉네임이 이미 설정되었습니다."
            );
            return;
        }

        string nickname = message.Content.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
        {
            await SendErrorAsync(
                session,
                "닉네임을 입력하세요."
            );
            return;
        }

        await session.SendAsync(new ChatMessage
        {
            Type = MessageType.NicknameResult,
            Sender = "Server",
            Content = $"{nickname} 닉네임으로 서버에 접속했습니다."
        });

        session.SetNickname(nickname);
    }

    private async Task HandleCancelLoginAsync(
        ClientSession session)
    {
        if (session.IsAuthenticated)
        {
            await SendErrorAsync(
                session,
                "서버 접속 후에는 로그인을 취소할 수 없습니다."
            );
            return;
        }

        session.CancelLogin();
    }

    private static Task SendErrorAsync(
        ClientSession session,
        string content) =>
        session.SendAsync(new ChatMessage
        {
            Type = MessageType.Error,
            Sender = "Server",
            Content = content
        });

    private Task HandleLeaveAsync(ChatMessage message) =>
        BroadcastAsync(message);

    private Task HandleCreateRoomAsync(ChatMessage message) =>
        BroadcastAsync(message);

    private Task HandleJoinRoomAsync(ChatMessage message) =>
        BroadcastAsync(message);

    private Task HandleGameReadyAsync(ChatMessage message) =>
        BroadcastAsync(message);

    private Task HandleMovePieceAsync(ChatMessage message) =>
        BroadcastAsync(message);

    private Task HandleErrorAsync(ChatMessage message) =>
        BroadcastAsync(message);

    private async Task BroadcastAsync(
        ChatMessage message)
    {
        List<ClientSession> clients;
        lock (_clientsLock)
        {
            clients = _clients
                .Where(session => session.IsAuthenticated)
                .ToList();
        }
        foreach(ClientSession session in clients)
        {
            try
            {
                await session.SendAsync(message);
            }
            catch (IOException)
            {
                // 전송 직전에 연결이 종료된 세션은 무시
            }
            catch (ObjectDisposedException)
            {
                // 이미 정리된 세션은 무시
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();

        List<ClientSession> clients;

        lock (_clientsLock)
        {
            clients = _clients.ToList();
            _clients.Clear();
        }

        foreach (ClientSession session in clients)
        {
            session.Dispose();
        }
    }
}
