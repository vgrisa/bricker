using Bricker.Api.Models;
using Bricker.Api.Services;

namespace Bricker.Api.Tests;

public sealed class InterestCreationPolicyTests
{
    [Fact]
    public void Evaluate_returns_listing_unavailable_when_listing_is_missing_or_inactive()
    {
        Assert.Equal(InterestCreationDecision.ListingUnavailable, InterestCreationPolicy.Evaluate(null, "buyer"));

        var inactiveListing = CreateListing("seller", ListingStatus.Inactive);
        Assert.Equal(InterestCreationDecision.ListingUnavailable, InterestCreationPolicy.Evaluate(inactiveListing, "buyer"));
    }

    [Fact]
    public void Evaluate_rejects_the_listing_owner()
    {
        var listing = CreateListing("seller", ListingStatus.Active);

        Assert.Equal(InterestCreationDecision.OwnListing, InterestCreationPolicy.Evaluate(listing, "seller"));
    }

    [Fact]
    public void Evaluate_rejects_listing_without_available_seller()
    {
        var listing = CreateListing(null, ListingStatus.Active);

        Assert.Equal(InterestCreationDecision.SellerUnavailable, InterestCreationPolicy.Evaluate(listing, "buyer"));
    }

    [Fact]
    public void Evaluate_allows_another_user_to_contact_an_active_listing()
    {
        var listing = CreateListing("seller", ListingStatus.Active);

        Assert.Equal(InterestCreationDecision.Allowed, InterestCreationPolicy.Evaluate(listing, "buyer"));
    }

    private static Listing CreateListing(string? sellerId, ListingStatus status) => new()
    {
        CategoryId = Guid.NewGuid(),
        Title = "Material de teste",
        Description = "Descrição de teste",
        Price = 100,
        Unit = "unidade",
        Quantity = 1,
        City = "Brusque",
        State = "SC",
        SellerDisplayName = "Vendedor",
        SellerId = sellerId,
        Status = status
    };
}
