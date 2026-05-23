namespace Clients.Domain.Entities;

public class Client
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string? ProfilePictureUrl { get; private set; }
    public BankingDetails BankingDetails { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Client() { }

    public static Client Create(string name, string email, string address, string agency, string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Name is required.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new Exceptions.DomainException("A valid email is required.");

        return new Client
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Address = address,
            BankingDetails = new BankingDetails(agency, accountNumber),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdatePartial(string? name, string? email, string? address, BankingDetails? bankingDetails)
    {
        if (name is not null) Name = name;
        if (email is not null) Email = email;
        if (address is not null) Address = address;
        if (bankingDetails is not null) BankingDetails = bankingDetails;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfilePicture(string pictureUrl)
    {
        ProfilePictureUrl = pictureUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}

public record BankingDetails(string Agency, string AccountNumber);
