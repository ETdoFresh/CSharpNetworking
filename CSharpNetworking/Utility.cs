using System.Collections.Generic;
using System.Linq;

namespace CSharpNetworking
{
    static class Utility
    {
       public static int IndexOf<T>(this IEnumerable<T> haystack, IEnumerable<T> needle)
{
    var needleList = needle.ToList();
    var haystackList = haystack.ToList();
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
    }
}