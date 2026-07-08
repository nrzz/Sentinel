using Sentinel.Infrastructure.Identity;

namespace Sentinel.UnitTests.Identity;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsNonEmptyBcryptHash()
    {
        var hash = _hasher.Hash("SecurePassword123!");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void Hash_ThrowsForNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(""));
        Assert.Throws<ArgumentException>(() => _hasher.Hash("   "));
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        const string password = "MyTestPassword!";
        var hash = _hasher.Hash(password);

        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForIncorrectPassword()
    {
        var hash = _hasher.Hash("CorrectPassword");

        Assert.False(_hasher.Verify("WrongPassword", hash));
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("", "hash")]
    [InlineData("password", null)]
    [InlineData("password", "")]
    public void Verify_ReturnsFalseForInvalidInputs(string? password, string? hash)
    {
        Assert.False(_hasher.Verify(password!, hash!));
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForSamePassword()
    {
        const string password = "SamePassword";
        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.Verify(password, hash1));
        Assert.True(_hasher.Verify(password, hash2));
    }
}
