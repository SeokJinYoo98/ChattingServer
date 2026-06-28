using System.Net.Sockets;
using YuJanggiCommon;

public static class Program
{
    private const string ServerAddress = "127.0.0.1";
    private const int ServerPort = 7777;

    public static async Task Main(string[] args)
    {
        string playerName = GetPlayerName(args);

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Console.WriteLine("플레이어 이름이 필요합니다.");
            return;
        }

        using TcpClient client = new();

        try
        {
            await client.ConnectAsync(ServerAddress, ServerPort);
            Console.WriteLine(
                $"서버에 연결했습니다: {ServerAddress}:{ServerPort}"
            );

            NetworkStream stream = client.GetStream();

            if (!await JoinAsync(stream, playerName))
            {
                return;
            }

            if (!SelectMatchmaking())
            {
                return;
            }

            MatchFoundResponse? match = await MatchmakeAsync(stream);

            if (match is null)
            {
                return;
            }

            GameStartEvent? gameStart =
                await ReceiveGameStartAsync(stream);

            if (gameStart is null)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine("=== 채팅방 입장 ===");
            Console.WriteLine($"게임 ID: {match.GameId}");
            Console.WriteLine(
                $"상대: {match.Opponent.PlayerName} | 진영: {match.Side}"
            );
            PrintBoard(gameStart);
            Console.WriteLine("명령: select x z | move x z | /quit");
            Console.WriteLine("그 외 입력은 채팅으로 전송됩니다.");

            using CancellationTokenSource chatCancellation = new();
            Task receiveTask = ReceiveChatAsync(
                stream,
                gameStart.Side,
                chatCancellation
            );

            await SendChatAsync(
                stream,
                chatCancellation.Token
            );

            chatCancellation.Cancel();
            client.Close();
            await receiveTask;
        }
        catch (SocketException exception)
        {
            Console.WriteLine($"서버 연결 오류: {exception.Message}");
        }
        catch (IOException exception)
        {
            Console.WriteLine($"통신이 종료되었습니다: {exception.Message}");
        }
        catch (InvalidDataException exception)
        {
            Console.WriteLine($"잘못된 서버 메시지: {exception.Message}");
        }
    }

    private static string GetPlayerName(string[] args)
    {
        if (args.Length > 0)
        {
            return args[0].Trim();
        }

        Console.Write("플레이어 이름: ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static bool SelectMatchmaking()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. 자동 매칭 시작");
            Console.WriteLine("0. 종료");
            Console.Write("선택: ");

            string? input = Console.ReadLine()?.Trim();

            if (input == "1")
            {
                return true;
            }

            if (input == "0" || input is null)
            {
                return false;
            }

            Console.WriteLine("1 또는 0을 입력하세요.");
        }
    }

    private static async Task<bool> JoinAsync(
        NetworkStream stream,
        string playerName)
    {
        string requestId = CreateRequestId();

        await SendAsync(
            stream,
            ChatMessage.Create(
                MessageType.Join,
                requestId,
                new JoinRequest(playerName)
            )
        );

        ChatMessage response = await ReceiveAsync(stream);

        if (response.Type == MessageType.Error)
        {
            PrintError(response);
            return false;
        }

        if (response.Type != MessageType.Join)
        {
            throw new InvalidDataException(
                $"Join 응답 대신 {response.Type} 메시지를 받았습니다."
            );
        }

        JoinResponse join = response.GetPayload<JoinResponse>();
        Console.WriteLine(
            $"참가 완료: {join.PlayerName} ({join.PlayerId})"
        );
        return true;
    }

    private static async Task<MatchFoundResponse?> MatchmakeAsync(
        NetworkStream stream)
    {
        string requestId = CreateRequestId();

        await SendAsync(
            stream,
            ChatMessage.Create(
                MessageType.MatchmakingStart,
                requestId,
                new MatchmakingStartRequest()
            )
        );

        Console.WriteLine("매칭을 시작했습니다.");

        while (true)
        {
            ChatMessage response = await ReceiveAsync(stream);

            switch (response.Type)
            {
                case MessageType.MatchmakingStatus:
                {
                    MatchmakingStatusResponse status =
                        response.GetPayload<MatchmakingStatusResponse>();

                    Console.WriteLine(
                        status.State == MatchmakingState.Waiting
                            ? "상대를 기다리는 중입니다."
                            : "매칭이 취소되었습니다."
                    );
                    break;
                }
                case MessageType.MatchFound:
                    Console.WriteLine("매칭에 성공했습니다.");
                    return response.GetPayload<MatchFoundResponse>();
                case MessageType.Error:
                    PrintError(response);
                    return null;
                default:
                    throw new InvalidDataException(
                        $"매칭 중 처리할 수 없는 메시지입니다: {response.Type}"
                    );
            }
        }
    }

    private static async Task<GameStartEvent?> ReceiveGameStartAsync(
        NetworkStream stream)
    {
        ChatMessage response = await ReceiveAsync(stream);

        if (response.Type == MessageType.Error)
        {
            PrintError(response);
            return null;
        }

        if (response.Type != MessageType.GameStart)
        {
            throw new InvalidDataException(
                $"GameStart 대신 {response.Type} 메시지를 받았습니다."
            );
        }

        return response.GetPayload<GameStartEvent>();
    }

    private static void PrintBoard(GameStartEvent gameStart)
    {
        PrintBoard(
            gameStart.Side,
            gameStart.CurrentTurn,
            gameStart.Pieces
        );
    }

    private static void PrintBoard(
        PlayerSide side,
        PlayerSide currentTurn,
        IReadOnlyList<BoardPieceState> pieces)
    {
        const int width = 9;
        const int height = 10;
        string[,] board = new string[height, width];

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                board[z, x] = "..";
            }
        }

        foreach (BoardPieceState piece in pieces)
        {
            if (piece.X < 0 || width <= piece.X ||
                piece.Z < 0 || height <= piece.Z)
            {
                throw new InvalidDataException(
                    $"보드 범위를 벗어난 기물 좌표입니다: " +
                    $"({piece.X}, {piece.Z})"
                );
            }

            board[piece.Z, piece.X] = GetPieceToken(piece);
        }

        Console.WriteLine();
        Console.WriteLine(
            $"내 진영: {side} | 현재 턴: {currentTurn}"
        );
        Console.WriteLine("    0  1  2  3  4  5  6  7  8");

        for (int z = 0; z < height; z++)
        {
            Console.Write($"{z,2}  ");

            for (int x = 0; x < width; x++)
            {
                Console.Write(board[z, x]);

                if (x < width - 1)
                {
                    Console.Write(' ');
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }

    private static string GetPieceToken(BoardPieceState piece)
    {
        char side = piece.Side == PlayerSide.Cho ? 'C' : 'H';
        char pieceType = piece.PieceType switch
        {
            GamePieceType.King => 'K',
            GamePieceType.Chariot => 'R',
            GamePieceType.Cannon => 'C',
            GamePieceType.Horse => 'H',
            GamePieceType.Elephant => 'E',
            GamePieceType.Guard => 'G',
            GamePieceType.Soldier => 'S',
            _ => throw new InvalidDataException(
                $"지원하지 않는 기물입니다: {piece.PieceType}"
            )
        };

        return $"{side}{pieceType}";
    }
    private static async Task SendChatAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        BoardPosition? selectedPosition = null;

        try
        {
            while (true)
            {
                string? input = await Console.In.ReadLineAsync(
                    cancellationToken
                );

                if (input is null ||
                    input.Equals(
                        "/quit",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                string[] parts = input.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (TryParsePositionCommand(
                    parts,
                    "select",
                    out BoardPosition selected
                ))
                {
                    selectedPosition = selected;

                    await SendAsync(
                        stream,
                        ChatMessage.Create(
                            MessageType.LegalMovesRequest,
                            CreateRequestId(),
                            new LegalMovesRequest(selected)
                        )
                    );
                    continue;
                }

                if (parts.Length > 0 &&
                    parts[0].Equals(
                        "move",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    if (selectedPosition is null)
                    {
                        Console.WriteLine(
                            "먼저 select x z로 기물을 선택하세요."
                        );
                        continue;
                    }

                    if (!TryParsePositionCommand(
                        parts,
                        "move",
                        out BoardPosition destination
                    ))
                    {
                        Console.WriteLine(
                            "사용법: move x z"
                        );
                        continue;
                    }

                    await SendAsync(
                        stream,
                        ChatMessage.Create(
                            MessageType.MoveRequest,
                            CreateRequestId(),
                            new MoveRequest(
                                selectedPosition,
                                destination
                            )
                        )
                    );

                    selectedPosition = null;
                    continue;
                }

                if (parts.Length > 0 &&
                    parts[0].Equals(
                        "select",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    Console.WriteLine("사용법: select x z");
                    continue;
                }

                await SendAsync(
                    stream,
                    ChatMessage.Create(
                        MessageType.GameChatSend,
                        CreateRequestId(),
                        new GameChatSendRequest(input)
                    )
                );
            }
        }
        catch (OperationCanceledException)
        {
            // 상대 퇴장 알림으로 입력이 취소된 경우
        }
    }

    private static bool TryParsePositionCommand(
        string[] parts,
        string command,
        out BoardPosition position)
    {
        position = null!;

        if (parts.Length != 3 ||
            !parts[0].Equals(
                command,
                StringComparison.OrdinalIgnoreCase
            ) ||
            !int.TryParse(parts[1], out int x) ||
            !int.TryParse(parts[2], out int z))
        {
            return false;
        }

        position = new BoardPosition(x, z);
        return true;
    }

    private static async Task ReceiveChatAsync(
        NetworkStream stream,
        PlayerSide playerSide,
        CancellationTokenSource cancellation)
    {
        try
        {
            while (true)
            {
                ChatMessage message = await ReceiveAsync(stream);

                if (message.Type == MessageType.LegalMovesResult)
                {
                    LegalMovesResult legalMoves =
                        message.GetPayload<LegalMovesResult>();

                    string destinations = legalMoves.LegalMoves.Count == 0
                        ? "없음"
                        : string.Join(
                            ", ",
                            legalMoves.LegalMoves.Select(position =>
                                $"({position.X}, {position.Z})"
                            )
                        );

                    Console.WriteLine(
                        $"선택 ({legalMoves.From.X}, " +
                        $"{legalMoves.From.Z}) 이동 가능: " +
                        destinations
                    );
                    continue;
                }

                if (message.Type == MessageType.MoveResult)
                {
                    MoveResultEvent move =
                        message.GetPayload<MoveResultEvent>();

                    Console.WriteLine(
                        $"이동: ({move.From.X}, {move.From.Z}) -> " +
                        $"({move.To.X}, {move.To.Z})"
                    );
                    PrintBoard(
                        playerSide,
                        move.CurrentTurn,
                        move.Pieces
                    );
                    continue;
                }

                if (message.Type == MessageType.GameChatReceived)
                {
                    GameChatReceivedEvent chat =
                        message.GetPayload<GameChatReceivedEvent>();

                    Console.WriteLine(
                        $"[{chat.SentAt.ToLocalTime():HH:mm:ss}] " +
                        $"{chat.SenderPlayerName}: {chat.Message}"
                    );
                    continue;
                }

                if (message.Type == MessageType.GameEnd)
                {
                    GameEndEvent gameEnd =
                        message.GetPayload<GameEndEvent>();

                    Console.WriteLine();
                    Console.WriteLine($"게임 종료: {gameEnd.Message}");
                    cancellation.Cancel();
                    return;
                }

                if (message.Type == MessageType.Error)
                {
                    PrintError(message);
                    continue;
                }

                Console.WriteLine(
                    $"처리하지 않은 서버 메시지: {message.Type}"
                );
            }
        }
        catch (IOException)
        {
            Console.WriteLine("서버와의 연결이 종료되었습니다.");
        }
        catch (ObjectDisposedException)
        {
            // 사용자가 /quit으로 클라이언트를 종료한 경우
        }
    }

    private static async Task SendAsync(
        NetworkStream stream,
        ChatMessage message)
    {
        byte[] packet = MessageProtocol.Encode(message);
        await stream.WriteAsync(packet);
    }

    private static async Task<ChatMessage> ReceiveAsync(
        NetworkStream stream)
    {
        byte[] header = new byte[MessageProtocol.HeaderSize];
        await stream.ReadExactlyAsync(header);

        int bodyLength = MessageProtocol.DecodeBodyLength(header);
        byte[] body = new byte[bodyLength];
        await stream.ReadExactlyAsync(body);

        return MessageProtocol.DecodeBody(body);
    }

    private static void PrintError(ChatMessage message)
    {
        ErrorResponse error = message.GetPayload<ErrorResponse>();
        Console.WriteLine($"오류 [{error.Code}]: {error.Message}");
    }

    private static string CreateRequestId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
