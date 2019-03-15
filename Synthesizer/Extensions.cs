using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis
{
    public static class EnumHelper
    {
        public static T GetEnumValue<T>(string term)
        {
            foreach (T argType in Enum.GetValues(typeof(T)))
            {
                var check = argType.ToString().ToLower().Equals(term);
                if (check)
                    return argType;
            }
            return default(T);
        }
        public static T ToEnum<T>(string value)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            catch(Exception)
            {
                return default(T);
            }
        }
        public static string AsString(this Enum e)
        {
            return e.ToString().ToLower();
        }

    }
    public static class Extensions
    {
        public static string Remove(this string myString, string term)
        {
            return myString.Replace(term, "");
        }
        public static UnSatCore AsUnSATCore(this List<UnSatCoreClause> list)
        {
            return new UnSatCore(list);
        }

        public static UnSatCores AsUnSATCores(this List<UnSatCore> list)
        {
            return new UnSatCores (list);
        }

        public static Lemmas AsLemmas(this List<Lemma> list)
        {
            return new Lemmas(list);
        }

        public static Lemma AsLemma(this List<LemmaClause> list)
        {
            return new Lemma(list);
        }

        public static LemmaClause AsLemmaClause(this List<BoolExpr> list)
        {
            return new LemmaClause(list);
        }

        public static void Times(this int count, Action action)
        {
            for (int i = 0; i < count; i++)
            {
                action();
            }
        }

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

        public static T ContainsWhich<T>(this string term, Dictionary<T, string> separators)
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
