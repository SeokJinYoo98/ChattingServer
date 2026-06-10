using MyServer;
public static class Program
{
    public static async Task Main(string[] args)
    {
        ChatServer server = new ChatServer(7777);

        Console.WriteLine("Chat Server Start");
        Console.WriteLine("Port: 7777");

        await server.StartAsync();
    }
}