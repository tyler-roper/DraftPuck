namespace DraftPuck.Shared.System;

public partial class ErrorLogEntity
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public string Error { get; set; } = null!;

    public string Info { get; set; } = null!;
}
