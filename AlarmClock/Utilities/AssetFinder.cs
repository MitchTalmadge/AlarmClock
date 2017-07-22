using System;

namespace AlarmClock.Utilities
{
    public class AssetFinder
    {
        public static Uri PathToUri(string path)
        {
            return new Uri("pack://application:,,,/"+path);
        }
    }
}