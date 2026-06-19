using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace ChatCommon;

public enum MessageType
{
    Chat,
    System,
    Join,
    Leave,
    CreateRoom,
    JoinRoom,
    GameReady,
    MovePiece,
    Error
};

public sealed class ChatMessage
{
    public MessageType Type { get; set; }
    public string Sender { get; set; }    = string.Empty;
    public string Content { get; set; }   = string.Empty;
}
public static class MessageProtocol
{
    // int는 4바이트입니다.
    public const int HeaderSize = sizeof(int);

    // 비정상적으로 큰 메시지를 차단합니다.
    public const int MaxBodySize = 4 * 1024;

    public static byte[] Encode(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

        if (body.Length > MaxBodySize)
        {
            throw new ArgumentException(
                $"메시지가 너무 큽니다. 최대 크기: {MaxBodySize} bytes"
            );
        }

        byte[] packet = new byte[HeaderSize + body.Length];

        BinaryPrimitives.WriteInt32BigEndian(
            packet.AsSpan(0, HeaderSize),
            body.Length
        );

        body.CopyTo(packet.AsSpan(HeaderSize));

        return packet;
    }

    public static int DecodeBodyLength(ReadOnlySpan<byte> header)
    {
        if (header.Length != HeaderSize)
        {
            throw new ArgumentException(
                $"헤더 크기는 반드시 {HeaderSize} bytes여야 합니다."
            );
        }

        int bodyLength = BinaryPrimitives.ReadInt32BigEndian(header);

        if (bodyLength <= 0 || bodyLength > MaxBodySize)
        {
            throw new InvalidDataException(
                $"유효하지 않은 본문 크기입니다: {bodyLength}"
            );
        }

        return bodyLength;
    }

    public static ChatMessage DecodeBody(ReadOnlySpan<byte> body)
    {
        if (body.Length <= 0 || body.Length > MaxBodySize)
        {
            throw new InvalidDataException(
                $"유효하지 않은 본문 크기입니다: {body.Length}"
            );
        }

        return JsonSerializer.Deserialize<ChatMessage>(body)
            ?? throw new InvalidDataException(
                "메시지 본문을 역직렬화할 수 없습니다."
            );
    }
}
