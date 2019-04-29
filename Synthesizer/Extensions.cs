using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        public static int ToInt(this string myString)
        {
            return Int32.Parse(myString);
        }
        public static string Remove(this string myString, string term)
        {
            return myString.Replace(term, "");
        }

        public static string Remove(this string myString, string[] terms)
        {
            terms.Length.Times((i)=>
            {
                myString = myString.Replace(terms[i], "");
            });
            return myString;
        }

        public static UnSatCore AsUnSATCore(this List<UnSatCoreClause> list)
        {
            return new UnSatCore(list);
        }

        public static UnSatCores AsUnSATCores(this List<UnSatCore> list)
        {
            return new UnSatCores (list);
        }

        public static Lemmas AsLemmas(this IEnumerable<Lemma> list)
        {
            return new Lemmas(list);
        }

        public static Lemma AsLemma(this IEnumerable<LemmaClause> list)
        {
            return new Lemma(list);
        }

        public static LemmaClause AsLemmaClause(this IEnumerable<BoolExpr> list)
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

        public static void Times(this int count, Action<int> action, int i = 0)
        {
            for (i = 0; i < count; i++)
            {
                action(i);
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
    public static class EnumerableExtensions
    {
        //public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        //{
        //    return source.Shuffle(new Random());
        //}

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng = null)
        {
            if (source == null) throw new ArgumentNullException("source");
            //if (rng == null) throw new ArgumentNullException("rng");
            if (rng == null)
                rng = new Random();

            return source.ShuffleIterator(rng);
        }

        private static IEnumerable<T> ShuffleIterator<T>(
            this IEnumerable<T> source, Random rng)
        {
            List<T> buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }
    }
    public static class DelegateExtension
    {
        public static Delegate CreateDelegate(this MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }

            return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
        }
    }
    public static class RandomExtension
    {

        //public static T EnumValue<T>(this Random random)
        //{
        //    var v = Enum.GetValues(typeof(T));
        //    return (T)v.GetValue(random.Next(v.Length));
        //}

        public static T EnumValue<T>(this Random random)
        {
            var enum_values = Enum.GetValues(typeof(T)).OfType<T>().ToList().Where(x => Convert.ToInt32(x) >= 0).ToList();
            var temp = enum_values.Count();
            return (T)enum_values[random.Next(0, enum_values.Count())];
        }

        public static List<int> InstantiateRandomly_List_Of_Int(this Random random, int limit)
        {

            var list = new List<int>();
            for (int i = 1; i <= limit; i++)
            {
                list.Add(i);
            }
            list = list.Shuffle(random).ToList();
            return list.GetRange(0, random.Next(limit));
        }

        public static int InstantiateRandomly_Int(this Random random, int limit)
        {
            return random.Next(limit);
        }
    }
}
