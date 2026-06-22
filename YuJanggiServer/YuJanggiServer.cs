using System.Net;
using System.Net.Sockets;
using MyServer.Client;
using YuJanggiCommon;

namespace MyServer;

public class YuJanggiServer
{
    private readonly List<ClientSession> _clients = new();
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
            lock (_clientsLock)
            {
                _clients.Remove(session);
            }

            session.Dispose();
            Console.WriteLine($"[Disconnect] {session.ClientInfo}");
        }
    }

    private Task DispatchMessageAsync(
        ClientSession session,
        ChatMessage message)
    {
        return message.Type switch
        {
            MessageType.Join or
            MessageType.CreateRoom or
            MessageType.JoinRoom or
            MessageType.Ready or
            MessageType.MoveRequest => SendErrorAsync(
                session,
                $"{message.Type} 처리는 아직 구현되지 않았습니다."
            ),
            MessageType.GameStart or
            MessageType.MoveResult or
            MessageType.TurnChanged or
            MessageType.GameEnd or
            MessageType.Error => SendErrorAsync(
                session,
                $"클라이언트가 보낼 수 없는 메시지 타입입니다: {message.Type}"
            ),
            _ => SendErrorAsync(
                session,
                $"지원하지 않는 메시지 타입입니다: {message.Type}"
            )
        };
    }

    private static Task SendErrorAsync(
        ClientSession session,
        string content)
    {
        return session.SendAsync(new ChatMessage
        {
            Type = MessageType.Error,
            Content = content
        });
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        ClearClients();
    }
}
