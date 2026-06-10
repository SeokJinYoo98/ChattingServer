
using System.Buffers.Binary;

using System.Net.Sockets;
using System.Text;

namespace MyServer
{
    public static class MessageProtocol
    {
        private const int PrefixSize = 4;
        private const int MaxMessageSize = 4096;

        public static async Task SendAsync(
                   NetworkStream stream,
                   string message)
        {
            byte[] body =
                Encoding.UTF8.GetBytes(message);

            if (body.Length == 0 ||
                MaxMessageSize < body.Length)
            {
                throw new IOException(
                    "메시지 크기 오류"
                );
            }

            byte[] packet =
                new byte[PrefixSize + body.Length];

            BinaryPrimitives.WriteInt32BigEndian(
                packet.AsSpan(0, PrefixSize),
                body.Length
            );

            body.AsSpan().CopyTo(
                packet.AsSpan(PrefixSize)
            );

            await stream.WriteAsync(packet);
        }

        public static async Task<string> ReceiveAsync(
            NetworkStream stream)
        {
            byte[] prefix =
                new byte[PrefixSize];

            await stream.ReadExactlyAsync(prefix);

            int bodyLength =
                BinaryPrimitives.ReadInt32BigEndian(
                    prefix
                );

            if (bodyLength <= 0 ||
                MaxMessageSize < bodyLength)
            {
                throw new IOException(
                    "잘못된 메시지 길이"
                );
            }

            byte[] body =
                new byte[bodyLength];

            await stream.ReadExactlyAsync(body);

            return Encoding.UTF8.GetString(body);
        }
    }
}