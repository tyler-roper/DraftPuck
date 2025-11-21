using DraftPuck.Shared.Titles;

namespace DraftPuck.Shared.Users;
public partial class UserTitleEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    public Guid TitleId { get; set; }
    public TitleEntity Title { get; set; } = null!;
    public bool IsEquipped { get; set; } = false;
}
