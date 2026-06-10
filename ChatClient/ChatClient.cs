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

        public async Task ConnectAsync(
            string host,
            int port)
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
                string? input = Console.ReadLine();

                if (input == "/exit")
                    break;

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                await MessageProtocol.SendAsync(
                    _stream,
                    input
                );
            }
        }

        public async Task ReceiveMessagesAsync()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException(
                    "ConnectAsync() must be called first."
                );
            }

            try
            {
                while (true)
                {
                    string message =
                        await MessageProtocol.ReceiveAsync(
                            _stream
                        );

                    Console.WriteLine(
                        $"[Receive] Length={message.Length}, Message=\"{message}\""
                    );
                }
            }
            catch (IOException)
            {
                Console.WriteLine(
                    "Disconnected from Server"
                );
            }
        }
        public void Disconnect()
        {
            _stream?.Close();
            _tcpClient.Close();
        }
        public void Dispose()
        {
            _stream?.Dispose();

            _tcpClient.Dispose();
        }
    }
}