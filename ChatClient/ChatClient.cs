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
        public async Task  RunAsync()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException(
                    "ConnectAsync() must be called first."
                );
            }

            while(true)
            {
                string? input = Console.ReadLine();

                if (input == "/exit")
                    break;

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var message = new ChatMessage
                {
                    Type    = MessageType.Chat,
                    Sender  = "Client",
                    Content = input
                };

                await SendAsync(message);
            }
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