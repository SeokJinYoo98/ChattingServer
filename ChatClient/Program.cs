using System.Threading.Tasks;
using ChatClient;

public static class Program
{
    private const string Host = "127.0.0.1";
    private const int Port = 7777;

    public static async Task Main()
    {
        using EchoClient client = new();

        await client.ConnectAsync(Host, Port);

        await client.RunAsync();
    }
}
