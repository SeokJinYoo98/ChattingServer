using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MyServer.Client
{
    public sealed class ClientSession : IDisposable
    {
        public TcpClient Client { get; }
        public NetworkStream Stream { get; }
        public string ClientInfo { get; }

        private readonly SemaphoreSlim _sendLock =
            new(1, 1);

        public ClientSession(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            ClientInfo =
                client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }

        public async Task SendAsync(string message)
        {
            await _sendLock.WaitAsync();

            try
            {
                await MessageProtocol.SendAsync(
                    Stream,
                    message
                );
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Dispose()
        {
            Stream.Dispose();
            Client.Dispose();
            _sendLock.Dispose();
        }
    }
}

