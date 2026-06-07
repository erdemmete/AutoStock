namespace AutoStock.Repositories.Constants
{
    public static class SqlDateTimeDefaults
    {
        public const string TurkeyNow =
            "CAST(SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Turkey Standard Time' AS datetime2)";
    }
}