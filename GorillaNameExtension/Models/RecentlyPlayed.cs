namespace GorillaNameExtension.Models;

public struct RecentlyPlayed
{
    public RecentState State;

    public float Value;

    private static readonly RecentlyPlayed never = new()
    {
        State = RecentState.Never,
        Value = 0
    };

    private static readonly RecentlyPlayed now = new()
    {
        State = RecentState.Now,
        Value = 0
    };

    public static RecentlyPlayed Never => never;
    public static RecentlyPlayed Now => now;
}
