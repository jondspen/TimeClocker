using System;

namespace TimeClocker.Utilities
{
    public static class CurrentTimeUtilty
    {
        public static DateTime ComputeCurrentTimeFromUTC()
        {
            var stringOffset = System.Configuration.ConfigurationManager.AppSettings["LocalTimeUtcOffset"].ToString();

            int result;
            if (int.TryParse(stringOffset, out result))
            {
                return DateTime.Now.ToUniversalTime().AddHours(result);
            }
            else
            {
                return DateTime.Now.ToUniversalTime().AddHours(-6);
            }
        }
    }
}