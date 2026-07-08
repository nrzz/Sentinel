namespace Sentinel.Infrastructure.Identity;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
