namespace PuyoDelivery.Core.Entities;

public class RestaurantAdmin : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Restaurant Restaurant { get; set; } = null!;
}
