namespace Bricker.Api.Models;

public sealed class Category
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public bool IsActive { get; init; } = true;
    public ICollection<Listing> Listings { get; init; } = new List<Listing>();
}
