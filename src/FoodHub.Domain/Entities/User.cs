using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities;

public class User
{
    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        FullName = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public string FullName { get; private set; }

    public Role Role { get; private set; }

    public static User Create(string email, string passwordHash, string fullName, Role role)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Full name is required.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role
        };
    }
}
