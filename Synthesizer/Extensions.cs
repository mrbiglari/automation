using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernateDemoApp
{
    public static class Extensions
    {
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
