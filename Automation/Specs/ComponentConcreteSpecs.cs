using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesizer
{
    public class ComponentConcreteSpecs
    {

        public static Int32 head(List<int> list)
        {
            if (list.Count == 0)
                throw new Exception("list is empty");
            return list.First();
        }

        public static Int32 last(List<int> list)
        {
            if (list.Count == 0)
                throw new Exception("list is empty");
            return list.Last();
        }

        public static List<int> take(List<int> list, int n)
        {
            if (list.Count < n)
                return list;
            return list.GetRange(0, n);
        }

        public static List<int> drop(List<int> list, int n)
        {
            if (list.Count < n)
                return null;
            return list.GetRange(n, list.Count - n);
        }
        public static int access(List<int> list, int n)
        {
            if (list.Count < n)
                throw new ArgumentException("index out of range");
            return list[n];
        }

        public static int minimum(List<int> list)
        {
            if (list.Count == 0)
                throw new Exception("list is empty");
            return list.Min();
        }

        public static int maximum(List<int> list)
        {
            if (list.Count == 0)
                throw new Exception("list is empty");
            return list.Max();
        }

        public static List<int> reverse(List<int> list)
        {
            list.Reverse();
            return list;
        }

        public static List<int> sort(List<int> list)
        {
            list.Sort();
            return list;
        }

        public static int sum(List<int> list)
        {
            if (list.Count == 0)
                return 0;
            return list.Sum();
        }


        public static int plus(int x, int y)
        {
            return x + y;
        }
        public static int minus(int x, int y)
        {
            return x - y;
        }
        public static int mul(int x, int y)
        {
            return x * y;
        }
        public static int div(int x, int y)
        {
            return x / y;
        }
        public static int pow(int x, int y)
        {
            return (int)Math.Pow(x, y);
        }
        public static int max(int x, int y)
        {
            return Math.Max(x, y);
        }
        public static int min(int x, int y)
        {
            return Math.Min(x, y);
        }
        //public delegate int Map_Int_To_Int(int x, int y);        
        public static List<int> map(List<int> list, Func<int, int, int> transform_IntToInt, int y)
        {
            return list.Select(x => transform_IntToInt(x, y)).ToList();
        }

        public static bool leq(int x, int y)
        {
            return x < y;
        }
        public static bool geq(int x, int y)
        {
            return x > y;
        }
        public static bool eq(int x, int y)
        {
            return x == y;
        }
        public static bool modeq(int x, int y)
        {
            return x % y == 0;
        }
        public static bool modneq(int x, int y)
        {
            return x % y != 0;
        }
        public delegate bool Predicate(int x, int y);
        public static List<int> filter(List<int> list, Func<int, int, bool> predicate, int y)
        {
            return list.Where(x => predicate(x, y)).ToList();
        }

        public static int count(List<int> list, Func<int, int, bool> predicate, int y)
        {
            return list.Count(x => predicate(x, y));
        }

        public delegate int Map_IntPairs_To_Int(int x, int y);
        public static List<int> zipWith(List<int> a, List<int> b, Func<int, int, int> map_IntPairs_To_Int)
        {
            return a.Zip(b, (first, second) =>
            {
                return map_IntPairs_To_Int(first, second);
            }).ToList();
        }

        public static List<int> scanL1(List<int> list, Func<int, int, int> map_IntPairs_To_Int)
        {
            var newList = new List<int>();
            newList.Add(list.First());

            for (int i = 1; i < list.Count; i++)
            {
                newList.Add(map_IntPairs_To_Int(newList[i - 1], list[i]));
            }
            return newList;
        }

    }
}
