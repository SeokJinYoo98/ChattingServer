using MyServer;
public static class Program
{
    public static async Task Main(string[] args)
    {
        ChatServer server = new ChatServer(7777);

        Console.WriteLine("Chat Server Start");
        Console.WriteLine("Port: 7777");
        Console.WriteLine("Commands: playerList, playerClear");

        Task serverTask = server.StartAsync();

        while (true)
        {
            string? command = Console.ReadLine()?.Trim();

            if (command == null)
            {
                server.Stop();
                break;
            }

            if (command.Equals(
                "playerList",
                StringComparison.OrdinalIgnoreCase))
            {
                server.PrintPlayerList();
                continue;
            }

            if (command.Equals(
                "playerClear",
                StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await server.ClearPlayersAsync();
                }
                catch (Exception exception)
                    when (exception is IOException or UnauthorizedAccessException)
                {
                    Console.WriteLine(
                        $"[PlayerClear Error] {exception.Message}"
                    );
                }
                continue;
            }

            if (!string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine("지원하지 않는 명령어입니다.");
            }
        }

        await serverTask;
    }
}
