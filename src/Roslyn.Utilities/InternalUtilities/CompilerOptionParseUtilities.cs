using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    public static class CompilerOptionParseUtilities
    {
        public static IList<string> ParseFeatureFromMSBuild(string features)
        {
            if (string.IsNullOrEmpty(features))
            {
                return new List<string>(0);
            }

            return features.Split(new[] {';', ',', ' '}, StringSplitOptions.RemoveEmptyEntries);
        }

        public static void ParseFeatures(IDictionary<string, string> builder, List<string> values)
        {
            foreach (string commaFeatures in values)
            {
                foreach (string feature in commaFeatures.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    ParseFeatureCore(builder, feature);
                }
            }
        }

        private static void ParseFeatureCore(IDictionary<string, string> builder, string feature)
        {
            int equals = feature.IndexOf('=');
            if (equals > 0)
            {
                string name = feature.Substring(0, equals);
                string value = feature.Substring(equals + 1);
                builder[name] = value;
            }
            else
            {
                builder[feature] = "true";
            }
        }
    }
}
