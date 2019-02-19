using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernateDemoApp
{
    public static class Extensions
    {
        public static List<T> Clone<T>(this List<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static List<int> FindAllIndexes<T>(this List<T> list, Predicate<T> match)
        {
            var tempList = list.ToList();
            
            var indexes = new List<int>();

            var tempCount = 0;
            while (tempList.FindIndex(match) != -1)
            {
                var index = tempList.FindIndex(match);
                indexes.Add(index + tempCount);

                tempCount += tempList.GetRange(0, index + 1).Count();
                tempList = tempList.GetRange(index + 1, tempList.Count - (index + 1));
            }

            return indexes;
        }

        public static List<string> SplitBy(this string term, string separator)
        {
            return term.Split(new string[] { separator }, StringSplitOptions.None).Select(x => x.Trim()).ToList();
        }
        public static string ContainsWhich(this string term, IEnumerable<string> separators)
        {
            var opr = String.Empty;
            foreach (var oprs in separators)
            {
                if (term.Contains(oprs))
                    opr = oprs;

            }
            return opr;
        }

        public static T ContainsWhich<T>(this string term, Dictionary<T,string> separators)
        {
            var operatorType = default(T);
            var keys = separators.Keys;

            foreach (var key in keys)
            {
                if (term.Contains(separators[key]))
                    operatorType = key;

            }
            return operatorType;
        }
    }
}
