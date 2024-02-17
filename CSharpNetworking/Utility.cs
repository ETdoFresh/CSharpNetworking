using System.Collections.Generic;

namespace CSharpNetworking
{
    static class Utility
    {
        public static int IndexOf<T>(this List<T> haystack, List<T> needle)
        {
            var needleList = needle;
            var haystackList = haystack;
            var needleCount = needleList.Count;
            var haystackCount = haystackList.Count;

            if (needleCount > haystackCount)
                return -1;

            for (var i = 0; i <= haystackCount - needleCount; i++)
            {
                var match = true;
                for (var j = 0; j < needleCount; j++)
                {
                    if (!haystackList[i + j].Equals(needleList[j]))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }

            return -1;
        }
        
        public static int IndexOf<T>(this IEnumerable<T> haystack, IEnumerable<T> needle)
        {
            return IndexOf(new List<T>(haystack), new List<T>(needle));
        }
        
        public static int IndexOf<T>(this IEnumerable<T> haystack, List<T> needle)
        {
            return IndexOf(new List<T>(haystack), needle);
        }
        
        public static int IndexOf<T>(this List<T> haystack, IEnumerable<T> needle)
        {
            return IndexOf(haystack, new List<T>(needle));
        }
    }
}