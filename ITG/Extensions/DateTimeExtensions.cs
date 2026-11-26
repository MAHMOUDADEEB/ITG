namespace ITG;

public static class DateTimeExtensions
{
    // Dates are saved in UTC. SQL Server does not keep the "UTC" mark, so a date read back
    // from the database must be marked as UTC before it is converted to local time.
    public static DateTime ToLocalFromUtc(this DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
}
