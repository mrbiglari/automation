using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesizer
{
    public class ComponentConcreteSpecs
    {

        public Int32 Head(List<int> list)
        {
            if (list.Count == 0)
                throw new Exception("list is empty");
            return list.First();
        }

        public Int32 Last(List<int> list)
        {
            if (list.Count == 0)
                throw new Exception("list is empty");
            return list.Last();
        }

        public List<int> Take(List<int> list, int n)
        {
            if (list.Count < n)
                return list;
            return list.GetRange(0, n - 1);
        }

        public List<int> Drop(List<int> list, int n)
        {
            if (list.Count < n)
                return null;
            return list.GetRange(n, list.Count);
        }
        public int Access(List<int> list, int n)
        {
            if (list.Count < n)
                throw new ArgumentException("index out of range");
            return list[n+1];
        }

        public int Minimum(List<int> list)
        {
            if (list.Count == 0)
                throw new Exception("list is empty");
            return list.Min();
        }

        public int Maximum(List<int> list)
        {
            if (list.Count == 0)
                throw new Exception("list is empty");
            return list.Max();
        }

        public List<int> Reverse(List<int> list)
        {
            list.Reverse();
            return list;
        }

        public List<int> Sort(List<int> list)
        {
            list.Sort();
            return list;
        }

        public int Sum(List<int> list)
        {
            if (list.Count == 0)
                return 0;
            return list.Sum();
        }

        public delegate int Map_Int_To_Int(int x);
        

        public List<int> Map(Map_Int_To_Int transform_IntToInt, List<int> list)
        {
            return list.Select(x => transform_IntToInt(x)).ToList();
        }

        public delegate bool Predicate(int x);
        public List<int> Filter(Predicate predicate, List<int> list)
        {
            return list.Where(x => predicate(x)).ToList();
        }

        public int Count(Predicate predicate, List<int> list)
        {
            return list.Count(x => predicate(x));
        }

        public delegate int Map_IntPairs_To_Int(int x, int y);
        public List<int> ZipWith(Map_IntPairs_To_Int map_IntPairs_To_Int, List<int> a, List<int> b)
        {
            return a.Zip(b, (first, second) =>
            {
                return map_IntPairs_To_Int(first, second);
            }).ToList();
        }

        public List<int> ScanL1(Map_IntPairs_To_Int map_IntPairs_To_Int, List<int> list)
        {
            var newList = new List<int>();
            newList.Add(list.First());

            for (int i = 1; i < list.Count; i++)
            {
                newList[i] = map_IntPairs_To_Int(newList[i - 1], list[i]);
            }
            return newList;
        }

    }
}
