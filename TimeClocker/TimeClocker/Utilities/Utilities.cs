using System;

namespace TimeClocker.Utilities
{
    public static class CurrentTimeUtilty
    {
        public static DateTime ComputeCurrentTimeFromUTC()
        {
            System.Configuration.Configuration rootWebConfig1 = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);
            if (rootWebConfig1.AppSettings.Settings.Count > 0)
            {
                System.Configuration.KeyValueConfigurationElement customSetting = rootWebConfig1.AppSettings.Settings["LocalTimeUtcOffset"];

                int result;
                if (int.TryParse(customSetting.Value, out result))
                {
                    return DateTime.Now.ToUniversalTime().AddHours(result);
                }
            }

            // Default to US Central Standard Time
            return DateTime.Now.ToUniversalTime().AddHours(-6);
        }
    }
}