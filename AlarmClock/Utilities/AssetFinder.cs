using System;

namespace AlarmClock.Utilities
{
    public class AssetFinder
    {
        public static Uri AssetToUri(string pathRelToAssets)
        {
            return new Uri("pack://application:,,,/assets/" + pathRelToAssets);
        }

        public static string AssetToPath(string pathRelToAssets)
        {
            return "assets/" + pathRelToAssets;
        }
    }
}