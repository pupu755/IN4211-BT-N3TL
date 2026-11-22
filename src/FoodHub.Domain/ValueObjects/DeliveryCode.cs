using System.Security.Cryptography;
using System.Text;
using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.ValueObjects;

/// <summary>
/// Six digit receipt code (QT03). Only a salted hash is kept. The code is single use,
/// expires after two hours and locks after five wrong attempts.
/// </summary>
public sealed class DeliveryCode
{
    public const int CodeLength = 6;
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan Validity = TimeSpan.FromHours(2);

    private DeliveryCode()
    {
        Salt = string.Empty;
        Hash = string.Empty;
    }

    public string Salt { get; private set; }

    public string Hash { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public int FailedAttempts { get; private set; }

    public bool IsUsed { get; private set; }

    public bool IsLocked => FailedAttempts >= MaxFailedAttempts;

    public static DeliveryCode Create(string plainCode, DateTime nowUtc)
    {
        if (!IsValidFormat(plainCode))
        {
            throw new DomainException("Delivery code must be exactly 6 digits.");
        }

        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        return new DeliveryCode
        {
            Salt = salt,
            Hash = ComputeHash(salt, plainCode),
            ExpiresAtUtc = nowUtc + Validity
        };
    }

    public CodeVerificationResult Verify(string input, DateTime nowUtc)
    {
        if (IsUsed)
        {
            return CodeVerificationResult.AlreadyUsed;
        }

        if (IsLocked)
        {
            return CodeVerificationResult.Locked;
        }

        if (nowUtc > ExpiresAtUtc)
        {
            return CodeVerificationResult.Expired;
        }

        var matches = IsValidFormat(input) && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(ComputeHash(Salt, input)),
            Encoding.UTF8.GetBytes(Hash));

        if (!matches)
        {
            FailedAttempts++;
            return CodeVerificationResult.Wrong;
        }

        IsUsed = true;
        return CodeVerificationResult.Success;
    }

    private static bool IsValidFormat(string? code) =>
        code is { Length: CodeLength } && code.All(char.IsAsciiDigit);

    private static string ComputeHash(string salt, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(salt + code)));
}
