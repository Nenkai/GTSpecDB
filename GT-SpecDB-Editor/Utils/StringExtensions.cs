using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GT_SpecDB_Editor.Utils
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
            => source?.IndexOf(toCheck, comp) >= 0;

        public static int CountCharOccurenceFromIndex(this string source, int index, char toCheck)
        {
            if (index > source.Length)
                return 0;

            int c = 0;
            for (int i = index; i < source.Length; i++)
            {
                if (source[i] == toCheck)
                    c++;
            }

            return c;
        }
    }
}
