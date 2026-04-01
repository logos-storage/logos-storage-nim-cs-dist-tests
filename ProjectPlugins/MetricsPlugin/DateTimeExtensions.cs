namespace MetricsPlugin
{
    public static class DateTimeExtensions
    {
        // Convert DateTime to Unix epoch seconds
        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
        }
    }
}
