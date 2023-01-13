using Microsoft.AspNetCore.JsonPatch.Operations;

namespace DraftPuck.Models.NhlApi
{
    public class Diffs
    {
        public List<Diff> Diff { get; set; } = null!;
    }

    public class Diff
    {
        public string Op { get; set; } = null!;
        public string Path { get; set; } = null!;
        public object? Value { get; set; }
        public string? From { get; set; }
    }
}
