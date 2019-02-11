using CSharpTree;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernateDemoApp
{
    public class SATEncoder<T>
    {
        //Intermediate Variable Symbol
        public const string ivs = "v";

        //Input Symbol
        public const string x = "x";

        //Output Symbol
        public const string y = "y";

        //Property Operator Symbol
        public const string ops = ".";

        public static string Before(TreeNode<T> node, string spec, int index)
        {
            string before;
            if (spec.Contains(ops))
                before = (node.Children.Count > 1) ? x + index + ops : x + ops;
            else
                before = (node.Children.Count > 1) ? x + index : x;

            return before;
        }

        public static string After(TreeNode<T> node, string spec, int index)
        {
            string after;
            if (spec.Contains(ops))
                after = ivs + node.Children.ElementAt(index - 1).index + ops;
            else
                after = ivs + node.Children.ElementAt(index - 1).index;

            return after;
        }

        public static string ReplaceInputSymbolsWithIntermediateVariables(TreeNode<T> node, string spec)
        {
            for (int i = 1; i < node.Children.Count + 1; i++)
            {
                var before = Before(node, spec, i);
                var after = After(node, spec, i);

                spec = spec.Replace(before, after);
            }

            return spec;
        }

        public static List<ComponentSpec> SATEncode(TreeNode<T> node, List<Tuple<string, string>> componentSpecs, Context context, List<ComponentSpec> satEncodingList = null)
        {
            if (satEncodingList == null)
                satEncodingList = new List<ComponentSpec>();

            var specAsString = componentSpecs.Where(x => x.Item1.Equals(node.Data)).FirstOrDefault();
            var spec = String.Empty;

            if (node.IsLeaf)
            {
                spec = node.Data.ToString() + RelationalOperators.operators[ERelationalOperators.Eq] + ivs + node.index;
            }
            else if (node.IsRoot)
            {
                spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.Item2);                
            }
            else
            {
                spec = spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.Item2);
                spec = spec.Replace(y, ivs + node.index);
            }

            var nodeSpec = ComponentSpecsBuilder.GetComponentSpec(Tuple.Create(node.Data.ToString(), spec));
            node.Spec = nodeSpec.spec;
            satEncodingList.Add(nodeSpec);

            foreach(var child in node.Children)
            {
                SATEncode(child, componentSpecs, context, satEncodingList);
            }

            return satEncodingList;
        }
    }
}
