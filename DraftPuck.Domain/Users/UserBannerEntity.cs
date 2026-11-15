using DraftPuck.Domain.Banners;

namespace DraftPuck.Domain.Users;
public partial class UserBannerEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    public Guid BannerId { get; set; }
    public BannerEntity Banner { get; set; } = null!;
    public bool IsEquipped { get; set; } = false;
}
