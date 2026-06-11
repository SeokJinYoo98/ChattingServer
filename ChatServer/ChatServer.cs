
using System.Net;
using System.Net.Sockets;
using MyServer.Client;
using ChatCommon;
namespace MyServer;
public class ChatServer
{
    private readonly List<ClientSession> _clients       = new();
    private readonly Lock                _clientsLock   = new();
    private readonly TcpListener         _listener;

    private bool _isRunning;

    public ChatServer(int port)
    {
        _listener = new TcpListener(
            IPAddress.Any,
            port
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

                lock (_clientsLock)
                {
                    _clients.Add(session);
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

                await BroadcastAsync(message);
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

            Console.WriteLine(
                $"[Disconnect] {session.ClientInfo}"
            );
        }
    }

    private async Task BroadcastAsync(
        ChatMessage message)
    {
        List<ClientSession> clients;
        lock (_clientsLock)
        {
            clients = _clients.ToList();
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
