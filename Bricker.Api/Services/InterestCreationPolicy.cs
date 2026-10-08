using Bricker.Api.Models;

namespace Bricker.Api.Services;

public enum InterestCreationDecision
{
    Allowed,
    ListingUnavailable,
    OwnListing,
    SellerUnavailable
}

public static class InterestCreationPolicy
{
    public static InterestCreationDecision Evaluate(Listing? listing, string userId)
    {
        if (listing is null || listing.Status != ListingStatus.Active)
            return InterestCreationDecision.ListingUnavailable;
        if (listing.SellerId == userId)
            return InterestCreationDecision.OwnListing;
        if (string.IsNullOrWhiteSpace(listing.SellerId))
            return InterestCreationDecision.SellerUnavailable;

        return InterestCreationDecision.Allowed;
    }
}
