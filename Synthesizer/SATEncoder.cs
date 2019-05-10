using CSharpTree;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Synthesis.UnSatCoreClause;

namespace Synthesis
{
    public enum ComponentType
    {
        Component,
        Parameter
    }
    public class Z3ComponentSpecs
    {
        public string key;
        public string value;
        public ComponentType type;

    }
    public class SATEncoder<T>
    {
        //Intermediate Variable Symbol
        //public const string ivs = "v";

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

        public static int calculateIndex(TreeNode<T> node, int index, Grammar grammar)
        {
            int retIndex = 0;
            //if (node.Parent != null)
            //{
                var parentPositionInRow = ((node.index) * grammar.maxArity);
                var currentNodePositionInParentsChildrenList = index;
                retIndex = currentNodePositionInParentsChildrenList + parentPositionInRow;
            //}
            return retIndex;
        }

        public static string After(TreeNode<T> node, string spec, int index, Grammar grammar, string interVar)
        {
            string after;
            if (spec.Contains($"{x}{ops}"))
                after = interVar + calculateIndex(node, index, grammar) + ops;
            else
                after = interVar + calculateIndex(node, index, grammar);

            return after;
        }

        public static string ReplaceInputSymbolsWithIntermediateVariables(TreeNode<T> node, string spec, Grammar grammar, string interVar)
        {
            for (int i = 1; i < node.Children.Count + 1; i++)
            {
                var before = Before(node, spec, i);
                var after = After(node, spec, i, grammar, interVar);

                spec = spec.Replace(before, after);
            }

            return spec;
        }

        public static string GetLeafSpec(ProgramSpec programSpec, TreeNode<T> node, string interVar)
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
                            + interVar + node.index + Symbols.dot + property);
                    }
                    break;

                case (Symbols.intType):
                    retSpecList.Add(node.Data.ToString() + RelationalOperators.operators[ERelationalOperators.Eq] + interVar + node.index);
                    break;

                case (Symbols.otherType):
                    retSpecList.Add(node.Data.ToString() + RelationalOperators.operators[ERelationalOperators.Eq] + interVar + node.index);
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
                var exampleSpecsAsBoolExprArray = example.Select(x => ComponentSpecsBuilder.GetComponentSpec(
                    new Z3ComponentSpecs()
                    {
                        key = $"parameter_{x.SplitBy(".").FirstOrDefault()}",
                        value = x
                    }).First()).ToList();
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

        public static BoolExpr SATEncode(TreeNode<T> node, Context context)
        {
            
                var expressions = satEncode(node, context);
            if (expressions.Any(x => x == null))
                ;
                if (expressions.Count > 1)
                return context.MkAnd(expressions);
            else
                return expressions.First();
        }

        public static List<BoolExpr> satEncode(TreeNode<T> node, Context context, List<BoolExpr> satEncoding = null)
        {
            if (satEncoding == null)
                satEncoding = new List<BoolExpr>();

            if (!node.IsHole)
                satEncoding.Add(node.expression);
            foreach (var child in node.Children)
            {
                if(!child.IsHole)
                    satEncode(child, context, satEncoding);
            }

            return satEncoding;
        }

        public static List<ProgramNode> SATEncodeTemp(TreeNode<T> node, ProgramSpec programSpec, List<Z3ComponentSpecs> componentSpecs, Context context, Grammar grammar, string interVar, List<ProgramNode> specList = null)
        {
            if (specList == null)
                specList = new List<ProgramNode>();

            var specAsString = componentSpecs.Where(x => x.key.Equals(node.Data)).FirstOrDefault();

            var spec = String.Empty;

            var nodeSpec = new List<BoolExpr>();
            if (specAsString == null)
            {
                var nodeSpecAsList = grammar.typeConstants.Where(x => x.Item1 == node.Data.ToString()).FirstOrDefault();
                if (nodeSpecAsList != null)
                {
                    switch (nodeSpecAsList.Item2.argType)
                    {
                        case (ArgType.List):
                            nodeSpec = ((List<string>)nodeSpecAsList.Item2.obj).Select(x => ComponentSpecsBuilder.GetSpecForClause1(x, node, interVar)).ToList();
                            break;

                        case (ArgType.Int):
                            nodeSpec.Add(ComponentSpecsBuilder.GetSpecForClause1(nodeSpecAsList.Item2.obj.ToString(), node, interVar));
                            break;
                    }
                }
                else
                {
                    nodeSpec.Add(context.MkBool(true));
                }
            }
            else
            {
                var check = componentSpecs.Select(x => x.key).Contains(node.Data.ToString());

                if (node.IsLeaf && node.IsRoot)
                {
                    spec = specAsString.value.Replace($"{Symbols.inputArg}{Symbols.dot}", $"{node.Data.ToString()}{Symbols.dot}");
                }
                else if (node.IsLeaf)
                {
                    //spec = GetLeafSpec(programSpec, node);                
                    //spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.Item2);

                    spec = specAsString.value.Replace($"{Symbols.outputArg}{Symbols.dot}", $"{interVar}{node.index}{Symbols.dot}");
                    spec = spec.Replace($"{Symbols.inputArg}{Symbols.dot}", $"{node.Data.ToString()}{Symbols.dot}");
                }
                else if (node.IsRoot)
                {
                    spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.value, grammar, interVar);
                }
                else
                {
                    spec = ReplaceInputSymbolsWithIntermediateVariables(node, specAsString.value, grammar, interVar);
                    spec = spec.Replace(y, interVar + node.index);
                }

                node.Spec = spec;

                nodeSpec = ComponentSpecsBuilder.GetComponentSpec(new Z3ComponentSpecs()
                {
                    key = node.Data.ToString(),
                    value = spec
                });
            }
            var nodeOriginalSpec = (specAsString != null) ?
            ComponentSpecsBuilder.GetComponentSpec(new Z3ComponentSpecs()
            {
                key = node.Data.ToString(),
                value = specAsString.value
            }) : null;

            //var nodeOriginalSpec = ComponentSpecsBuilder.GetComponentSpec(Tuple.Create(node.Data.ToString(), specAsString?.Item2??null));


            var storeSpec = new Pair<List<BoolExpr>, List<BoolExpr>>(nodeSpec, nodeOriginalSpec);

            specList.Add(new ProgramNode(node.Data.ToString(), node.index, storeSpec));

            foreach (var child in node.Children.Where(x => !x.IsHole))
            {
                SATEncodeTemp(child, programSpec, componentSpecs, context, grammar, interVar, specList);
            }

            return specList;
        }        

        public static SMTModel SMTEncode(List<Z3ComponentSpecs> componentSpecs, Context context, ProgramSpec programSpec, TreeNode<T> programRoot, Grammar grammar, string interVar)
        {
            return new SMTModel()
            {
                satEncodedProgram = SATEncodeProgram(componentSpecs, context, programSpec, programRoot, grammar, interVar),
                satEncodedProgramSpec = SATEncodeProgramSpec(context, programSpec)
            };
        }
        public static List<ProgramNode> SATEncodeProgram(List<Z3ComponentSpecs> componentSpecs, Context context, ProgramSpec programSpec, TreeNode<T> programRoot, Grammar grammar, string interVar)
        {
            var satEncodingList = SATEncodeTemp(programRoot, programSpec, componentSpecs, context, grammar, interVar);
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
