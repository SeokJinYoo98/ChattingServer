using System.Security.Cryptography;
using System.Text.Json;
using ChatCommon;

namespace MyServer;

public sealed class UserAccountStore
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public UserAccountStore(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "accounts.json");
    }

    public async Task<bool> RegisterAsync(UserAccount account)
    {
        await _fileLock.WaitAsync();

        try
        {
            List<StoredUserAccount> accounts = await LoadAsync();

            if (accounts.Any(savedAccount =>
                string.Equals(
                    savedAccount.UserId,
                    account.UserId,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] passwordHash = Rfc2898DeriveBytes.Pbkdf2(
                account.Password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            accounts.Add(new StoredUserAccount
            {
                UserId = account.UserId,
                PasswordHash = Convert.ToBase64String(passwordHash),
                PasswordSalt = Convert.ToBase64String(salt)
            });

            await using FileStream stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(
                stream,
                accounts,
                JsonOptions
            );

            return true;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<StoredUserAccount>> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<StoredUserAccount>();
        }

        await using FileStream stream = File.OpenRead(_filePath);

        try
        {
            return await JsonSerializer.DeserializeAsync<List<StoredUserAccount>>(
                stream,
                JsonOptions
            ) ?? new List<StoredUserAccount>();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "사용자 데이터 파일을 읽을 수 없습니다.",
                exception
            );
        }
    }

    private sealed class StoredUserAccount
    {
        public string UserId { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
    }
}
