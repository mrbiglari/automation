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
            if (spec.Contains($"{x}{ops}"))
                before = (node.Children.Count > 1) ? x + index + ops : x + ops;
            else
                before = (node.Children.Count > 1) ? x + index : x;

            return before;
        }

        public static string After(TreeNode<T> node, string spec, int index)
        {
            string after;
            if (spec.Contains($"{x}{ops}"))
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

        public static string GetLeafSpec(ProgramSpec programSpec, TreeNode<T> node)
        {
            var argIndex = Int32.Parse(node.Data.ToString().Replace(Symbols.inputArg, "") != node.Data.ToString() ?
                node.Data.ToString().Replace(Symbols.inputArg, "") :
                "-1");
            var argType = (argIndex != -1) ? programSpec.args[argIndex - 1].type : Symbols.otherType;

            var retSpecList = new List<string>();

            switch (argType)
            {
                case (Symbols.listType):
                    foreach (var property in Symbols.properties)
                    {
                        retSpecList.Add(node.Data.ToString() + Symbols.dot + property
                            + RelationalOperators.operators[ERelationalOperators.Eq]
                            + ivs + node.index + Symbols.dot + property);
                    }
                    break;

                case (Symbols.intType):
                    retSpecList.Add(node.Data.ToString() + RelationalOperators.operators[ERelationalOperators.Eq] + ivs + node.index);
                    break;

                case (Symbols.otherType):
                    retSpecList.Add(node.Data.ToString() + RelationalOperators.operators[ERelationalOperators.Eq] + ivs + node.index);
                    break;

                default:
                    break;
            }
            var retSpec = String.Join(LogicalOperators.operators[ELogicalOperators.AND], retSpecList);
            return retSpec;
        }

        public static List<ExampleNode> GetProgramSpecZ3Expression(List<List<string>> programSpecAsString, Context context)
        {
            var programSpecAsBoolExprList = new List<ExampleNode>();
            foreach (var example in programSpecAsString)
            {
                var exampleSpecsAsBoolExprArray = example.Select(x => ComponentSpecsBuilder.GetComponentSpec(Tuple.Create($"parameter_{x.SplitBy(".").FirstOrDefault()}", x)).First()).ToList();
                //var exampleSpecAsBoolExpr = context.MkAnd(exampleSpecsAsBoolExprArray);
                programSpecAsBoolExprList.Add(new ExampleNode(exampleSpecsAsBoolExprArray));
            }

            return programSpecAsBoolExprList;            
        }

        public static List<List<string>> GetProgramSpecZ3AsString(ProgramSpec programSpec)
        {
            var programSpecAsString = new List<List<string>>();

            foreach(var example in programSpec.examples)
            {
                var exampleSpecAsString = new List<string>();
                foreach (var parameter in example.parameters)
                {
                    switch(parameter.argType)
                    {
                        case (ArgType.List):
                            parameter.As<List<string>>().ForEach((x) =>
                            {
                                exampleSpecAsString.Add(x);
                            });
                            break;
                        case (ArgType.Int):
                            exampleSpecAsString.Add(parameter.As<string>());
                            break;

                    }
                }
                programSpecAsString.Add(exampleSpecAsString);
            }

            
            return programSpecAsString;
        }

        public static List<ProgramNode> SATEncodeTemp(TreeNode<T> node, ProgramSpec programSpec, List<Tuple<string, string>> componentSpecs, Context context, List<ProgramNode> specList = null)
        {
            if (specList == null)
                specList = new List<ProgramNode>();

            var specAsString = componentSpecs.Where(x => x.Item1.Equals(node.Data)).FirstOrDefault();
            var spec = String.Empty;

            if (node.IsLeaf)
            {
                spec = GetLeafSpec(programSpec, node);
            }
            else if (node.IsRoot)
            {
                spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.Item2);
            }
            else
            {
                spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.Item2);
                spec = spec.Replace(y, ivs + node.index);
            }

            node.Spec = spec;
            var nodeSpec = ComponentSpecsBuilder.GetComponentSpec(Tuple.Create(node.Data.ToString(), spec));

            specList.Add(new ProgramNode(node.Data.ToString(), node.index, nodeSpec));

            foreach (var child in node.Children)
            {
                SATEncodeTemp(child, programSpec, componentSpecs, context, specList);
            }

            return specList;
        }

        //public static List<ComponentSpec> GenerateZ3Expression(TreeNode<T> node, Context context, ProgramSpec programSpec, List<ComponentSpec> satEncodingList = null)
        //{
        //    if (satEncodingList == null)
        //        satEncodingList = new List<ComponentSpec>();

        //    var spec = node.Spec;

        //    if (node.IsLeaf)
        //    {
        //        spec = GetLeafSpec(programSpec, node);
        //    }

        //    var nodeSpec = ComponentSpecsBuilder.GetComponentSpec(Tuple.Create(node.Data.ToString(), spec));

        //    satEncodingList.Add(nodeSpec);
        //    foreach (var child in node.Children)
        //    {
        //        GenerateZ3Expression(child, context, programSpec, satEncodingList);
        //    }

        //    return satEncodingList;
        //}

        //public static List<string> SATEncode(TreeNode<T> node, List<Tuple<string, string>> componentSpecs, Context context, List<string> specList = null)
        //{
        //    if (specList == null)
        //        specList = new List<string>();

        //    var specAsString = componentSpecs.Where(x => x.Item1.Equals(node.Data)).FirstOrDefault();
        //    var spec = String.Empty;

        //    if (node.IsLeaf)
        //    {
        //        spec = node.Data.ToString() + RelationalOperators.operators[ERelationalOperators.Eq] + ivs + node.index;
        //    }
        //    else if (node.IsRoot)
        //    {
        //        spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.Item2);
        //    }
        //    else
        //    {
        //        spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.Item2);
        //        spec = spec.Replace(y, ivs + node.index);
        //    }

        //    node.Spec = spec;
        //    specList.Add(spec);
        //    foreach (var child in node.Children)
        //    {
        //        SATEncode(child, componentSpecs, context, specList);
        //    }

        //    return specList;
        //}

        public static SMTModel SATEncode(List<Tuple<string, string>> componentSpecs, Context context, ProgramSpec programSpec, TreeNode<T> programRoot)
        {
            return new SMTModel()
            {
                satEncodedProgram = SATEncodeProgram(componentSpecs, context, programSpec, programRoot),
                satEncodedProgramSpec = SATEncodeProgramSpec(context, programSpec)
            };
        }
        public static List<ProgramNode> SATEncodeProgram(List<Tuple<string, string>> componentSpecs, Context context, ProgramSpec programSpec, TreeNode<T> programRoot)
        {
            var satEncodingList = SATEncodeTemp(programRoot, programSpec, componentSpecs, context);

            //var satEncodings = GenerateZ3Expression(programRoot, context, programSpec);
            //var satEncoding = context.MkAnd(satEncodings.Select(x => x.spec).ToArray());
            //var satEncoding = context.MkAnd(satEncodingList.ToArray());
            return satEncodingList;
        }

        public static List<ExampleNode> SATEncodeProgramSpec(Context context, ProgramSpec programSpec)
        {
            var programSpecAsString = GetProgramSpecZ3AsString(programSpec);
            var programSpecAsZ3Expression = GetProgramSpecZ3Expression(programSpecAsString, context);

            return programSpecAsZ3Expression;
        }

    }
}
