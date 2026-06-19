using ChatCommon;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ChatClient
{

    public sealed class EchoClient : IDisposable
    {
        private readonly TcpClient _tcpClient = new();

        private NetworkStream? _stream;
        private string _nickname = string.Empty;

        public async Task  ConnectAsync(string host, int port)
        {
            await _tcpClient.ConnectAsync(
                host,
                port
            );

            _stream = _tcpClient.GetStream();

            Console.WriteLine(
                "Connected to Server"
            );
        }
        public async Task RunAsync()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException(
                    "ConnectAsync() must be called first."
                );
            }

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("1. 로그인");
                Console.WriteLine("2. 회원가입");
                Console.WriteLine("3. 종료");
                Console.Write("선택: ");

                string? input = Console.ReadLine()?.Trim();

                switch (input)
                {
                    case "1":
                    case "로그인":
                        if (await LoginAsync())
                        {
                            await RunConnectedAsync();
                            return;
                        }
                        break;
                    case "2":
                    case "회원가입":
                        await RegisterAsync();
                        break;
                    case "3":
                    case "종료":
                        return;
                    default:
                        Console.WriteLine("1, 2, 3 또는 메뉴 이름을 입력하세요.");
                        break;
                }
            }
        }

        private async Task<bool> LoginAsync()
        {
            Console.Write("아이디: ");
            string? userId = Console.ReadLine()?.Trim();

            Console.Write("비밀번호: ");
            string? password = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("아이디와 비밀번호를 모두 입력하세요.");
                return false;
            }

            await SendAsync(new ChatMessage
            {
                Type = MessageType.Login,
                Account = new UserAccount
                {
                    UserId = userId,
                    Password = password
                }
            });

            ChatMessage loginResult = await ReceiveAsync();
            Console.WriteLine(loginResult.Content);

            if (loginResult.Type != MessageType.LoginResult)
            {
                return false;
            }

            while (true)
            {
                Console.Write("닉네임: ");
                string? nickname = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(nickname))
                {
                    Console.WriteLine("닉네임을 입력하세요.");
                    continue;
                }

                await SendAsync(new ChatMessage
                {
                    Type = MessageType.SetNickname,
                    Content = nickname
                });

                ChatMessage nicknameResult = await ReceiveAsync();
                Console.WriteLine(nicknameResult.Content);

                if (nicknameResult.Type == MessageType.NicknameResult)
                {
                    _nickname = nickname;
                    return true;
                }
            }
        }

        private async Task RegisterAsync()
        {
            Console.Write("아이디: ");
            string? userId = Console.ReadLine()?.Trim();

            Console.Write("비밀번호: ");
            string? password = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("아이디와 비밀번호를 모두 입력하세요.");
                return;
            }

            var message = new ChatMessage
            {
                Type = MessageType.Register,
                Account = new UserAccount
                {
                    UserId = userId,
                    Password = password
                }
            };

            await SendAsync(message);
            ChatMessage result = await ReceiveAsync();
            Console.WriteLine(result.Content);
        }

        private async Task RunConnectedAsync()
        {
            Console.WriteLine("메시지를 입력하세요. 종료하려면 '종료'를 입력하세요.");

            Task receiveTask = ReceiveMessageAsync();

            while (true)
            {
                string? content = Console.ReadLine();

                if (content?.Trim() == "종료")
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                await SendAsync(new ChatMessage
                {
                    Type = MessageType.Chat,
                    Sender = _nickname,
                    Content = content
                });
            }

            Disconnect();
            await receiveTask;
        }
        public async Task  ReceiveMessageAsync()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException(
                    "ConnectAsync() must be called first."
                );
            }

            try
            {
                while(true)
                {
                    ChatMessage message = await ReceiveAsync();

                    Console.WriteLine(
                        $"[{message.Type}] {message.Sender}: {message.Content}");
                }
            }
            catch (EndOfStreamException)
            {
                Console.WriteLine(
                    "Disconnected from Server"
                );
            }
            catch (IOException)
            {
                Console.WriteLine(
                    "Disconnected from Server"
                );
            }
            catch (ObjectDisposedException)
            {
                // 클라이언트에서 정상적으로 연결을 종료한 경우
            }
        }



        private async Task              SendAsync(ChatMessage message)
        {
            if (_stream== null)
            {
                throw new InvalidOperationException(
                    "ConnectAsync() must be called first."
                );
            }
            byte[] packet = MessageProtocol.Encode(message);

            await _stream.WriteAsync(packet);
        }
        private async Task<ChatMessage> ReceiveAsync()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException(
                    "ConnectAsync() must be called first."
                );
            }

            byte[] header = new byte[MessageProtocol.HeaderSize];

            await _stream.ReadExactlyAsync(header);

            int bodyLength = MessageProtocol.DecodeBodyLength(header);

            byte[] body = new byte[bodyLength];

            await _stream.ReadExactlyAsync(body);

            return MessageProtocol.DecodeBody(body);
        }

        public void Disconnect()
        {
            _stream?.Dispose();
            _stream = null;

            _tcpClient.Dispose();
        }
        public void Dispose()
        {
            Disconnect();
        }
    }
}
