namespace PuyoDelivery.Core.Entities;

public enum RestaurantClaimStatus
{
    Pending,
    Approved,
    Rejected
}

public class RestaurantClaim : BaseEntity
{
    public Guid RestaurantId { get; set; }
    public Guid? ClaimantUserId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public RestaurantClaimStatus Status { get; set; } = RestaurantClaimStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? AdminNotes { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
}
