namespace skillexa_backend.Infrastructure.Data;

public sealed class AdminSeedOptions
{
    public bool Enabled { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
