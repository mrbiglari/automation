using CSharpTree;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Synthesis
{
    public enum ELogicalOperators
    {
        AND,
        OR,
        NOT,
        EQV
    }

    public enum ERelationalOperators
    {
        Gt,
        GtEq,
        L,
        LEq,
        Eq
    }

    public enum EArithmaticOperators
    {
        MUL,
        SUM,
        SUB,
        DIV
    }

    public interface IOperator<T>
    {
        string GetSymbolFor(T operatorKey);
    }

    public class Operators
    {
        public static string GetSymbolFor(ELogicalOperators operatorKey)
        {
            string symbol;
            LogicalOperators.operators.TryGetValue(operatorKey, out symbol);
            return symbol;
        }

        public static string GetSymbolFor(ERelationalOperators operatorKey)
        {
            string symbol;
            RelationalOperators.operators.TryGetValue(operatorKey, out symbol);
            return symbol;
        }

        public static string GetSymbolFor(EArithmaticOperators operatorKey)
        {
            string symbol;
            ArithmaticOperators.operators.TryGetValue(operatorKey, out symbol);
            return symbol;
        }
    }

    public class RelationalOperators
    {
        public static Dictionary<ERelationalOperators, string> operators = new Dictionary<ERelationalOperators, string>() {
            {ERelationalOperators.Eq, "="},
            {ERelationalOperators.Gt, ">"},
            {ERelationalOperators.GtEq, "≥"},
            {ERelationalOperators.L, "<"},
            {ERelationalOperators.LEq, "≤"},
        };
        public static BoolExpr GetSpec(ERelationalOperators opr, ArithExpr arg_1, ArithExpr arg_2, Context ctx)
        {
            switch (opr)
            {
                case (ERelationalOperators.Eq):
                    return ctx.MkEq(arg_1, arg_2);

                case (ERelationalOperators.Gt):
                    return ctx.MkGt(arg_1, arg_2);

                case (ERelationalOperators.L):
                    return ctx.MkLt(arg_1, arg_2);

                case (ERelationalOperators.GtEq):
                    var first = ctx.MkEq(arg_1, arg_2);
                    var second = ctx.MkGt(arg_1, arg_2);
                    return ctx.MkOr(first, second);

                case (ERelationalOperators.LEq):
                    var firsts = ctx.MkEq(arg_1, arg_2);
                    var seconds = ctx.MkLt(arg_1, arg_2);
                    return ctx.MkOr(firsts, seconds);
            }
            return null;
        }
    }

    public class LogicalOperators
    {
        public static Dictionary<ELogicalOperators, string> operators = new Dictionary<ELogicalOperators, string>() {
            {ELogicalOperators.AND, "∧"},
            {ELogicalOperators.NOT, "!"},
            {ELogicalOperators.OR, "∨"},
            {ELogicalOperators.EQV, "≡"},
        };

        public static BoolExpr GetSpec(ELogicalOperators opr, BoolExpr arg_1, BoolExpr arg_2, Context ctx)
        {
            switch (opr)
            {
                case (ELogicalOperators.AND):
                    return ctx.MkAnd(new BoolExpr[] { arg_1, arg_2 });

                case (ELogicalOperators.OR):
                    return ctx.MkOr(new BoolExpr[] { arg_1, arg_2 });

                case (ELogicalOperators.NOT):
                    return ctx.MkNot(arg_1);

                case (ELogicalOperators.EQV):
                    return ctx.MkEq(arg_1, arg_2);
            }
            return null;
        }
    }

    public class ArithmaticOperators
    {
        public static Dictionary<EArithmaticOperators, string> operators = new Dictionary<EArithmaticOperators, string>() {
            {EArithmaticOperators.SUM, "+"},
            {EArithmaticOperators.SUB, "-"},
            {EArithmaticOperators.MUL, "*"},
            {EArithmaticOperators.DIV, "/"},
        };

        public static ArithExpr GetSpec(EArithmaticOperators opr, ArithExpr arg_1, ArithExpr arg_2, Context ctx)
        {
            switch (opr)
            {
                case (EArithmaticOperators.SUM):
                    return ctx.MkAdd(arg_1, arg_2);

                case (EArithmaticOperators.SUB):
                    return ctx.MkSub(arg_1, arg_2);

                case (EArithmaticOperators.MUL):
                    return ctx.MkMul(arg_1, arg_2);

                case (EArithmaticOperators.DIV):
                    return ctx.MkDiv(arg_1, arg_2);
            }
            return null;
        }
    }

    public class ComponentSpecsBuilder
    {
        public const string key_componentSpec = "ComponentSpec";
        public const string key_name = "Name";
        public const string key_spec = "Spec";
        public static Context context;

        public static List<Z3ComponentSpecs> Build(string fileName, Context ctx, ProgramSpec programSpec, Grammar grammar)
        {
            context = ctx;
            var specContent = GetComponentSpecsFile(fileName);
            return BuildComponentSpecsFromSpec(specContent, programSpec, grammar);
        }

        private static XElement GetComponentSpecsFile(string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var componentSpecsFilepath = Path.Combine(currentDirectory, fileName);
            return XElement.Load(componentSpecsFilepath);
        }

        public static ArithExpr GetArg(string term)
        {
            int numeral;
            var isNumeric = int.TryParse(term, out numeral);

            if (isNumeric)
                return (ArithExpr)context.MkNumeral(numeral, context.MkIntSort());
            else
            {
                //if (term.Contains(".") && term.SplitBy(".").First().Contains("x"))
                return (ArithExpr)context.MkConst(term, context.MkIntSort());
            }
            //return null;
        }

        public static BoolExpr GetSpecForClause(string spec)
        {
            var opr = spec.ContainsWhich(RelationalOperators.operators);
            var operands = spec.SplitBy(RelationalOperators.operators[opr]);

            var arg_1 = GetArg(operands.First());

            var arg_2 = GetArg(operands.Last());

            var boolExpr = RelationalOperators.GetSpec(opr, arg_1, arg_2, context);

            return boolExpr;
        }

        public static string GetIntermediateVariableWithPropertyAsString<T>(TreeNode<T> node, List<string> operands, string spec, string interVar)
        {
            if (node.IsRoot)
            {
                return operands.First();
            }
            else
            {
                if (!spec.Contains(Symbols.dot))
                    return $"{interVar}{node.index.ToString()}";
                else
                    return $"{interVar}{node.index.ToString()}{Symbols.dot}{operands.First().SplitBy(Symbols.dot).Last()}";
            }
        }

        public static BoolExpr GetSpecForClause1<T>(string spec, TreeNode<T> node, string interVar)
        {
            var opr = spec.ContainsWhich(RelationalOperators.operators);
            var operands = spec.SplitBy(RelationalOperators.operators[opr]);

            var arg_1 = GetArg(GetIntermediateVariableWithPropertyAsString(node, operands, spec, interVar));

            var arg_2 = GetArg(operands.Last());

            var boolExpr = RelationalOperators.GetSpec(opr, arg_1, arg_2, context);

            return boolExpr;
        }


        public static List<BoolExpr> GetComponentSpec(Z3ComponentSpecs componentSpec)
        {
            var name = componentSpec.key;
            var specList = componentSpec.value.SplitBy(Operators.GetSymbolFor(ELogicalOperators.AND)).Select(x => x.Trim()).ToList();

            var z3SpecsList = new List<BoolExpr>();
            foreach (var spec in specList)
            {
                var boolExpr = GetSpecForClause(spec);
                z3SpecsList.Add(boolExpr);
            }
            //var z3Spec = (z3SpecsList.Count > 1) ? context.MkAnd(z3SpecsList.ToArray()) : z3SpecsList.First();

            //return new ComponentSpec(name, z3Spec);
            return z3SpecsList;
        }

        public static string GetSpecNonComponents(Parameter x)
        {
            var retSpecList = new List<string>();
            switch (x.argType)
            {
                case (ArgType.List):
                    foreach (var property in Symbols.properties)
                    {
                        retSpecList.Add($"{Symbols.outputArg}{Symbols.dot}{property} {RelationalOperators.operators[ERelationalOperators.Eq]} {Symbols.inputArg}{Symbols.dot}{property}");
                    }
                    break;

                case (ArgType.Int):
                    retSpecList.Add($"{Symbols.outputArg} {RelationalOperators.operators[ERelationalOperators.Eq]} {Symbols.inputArg}");
                    break;

                case (ArgType.Other):
                    retSpecList.Add($"{Symbols.outputArg} {RelationalOperators.operators[ERelationalOperators.Eq]} {Symbols.inputArg}");
                    break;

                default:
                    break;
            }
            return String.Join(" " + LogicalOperators.operators[ELogicalOperators.AND] + " ", retSpecList);
        }

        private static List<Z3ComponentSpecs> BuildComponentSpecsFromSpec(XElement componentSpecsXML, ProgramSpec programSpec, Grammar grammar)
        {
            var componentSpecsList = componentSpecsXML.Descendants(key_componentSpec)
                .Select(x =>
                    new Z3ComponentSpecs()
                    {
                        key = x.Descendants(key_name).FirstOrDefault().Value.Trim(),
                        value = x.Descendants(key_spec).FirstOrDefault().Value.Trim(),
                        type = ComponentType.Component
                    }
                ).ToList();

            var ret = programSpec.parameters.Where(x => x.parameterType == ParameterType.Input).Select(
                x => new Z3ComponentSpecs()
                {
                    key = x.obj.ToString(),
                    value = GetSpecNonComponents(x),
                    type = ComponentType.Parameter
                }
               ).ToList();
            //var ret2 = grammar.types.Select( x => Tuple.Create(x.Item1, x.Item2.obj);
            return componentSpecsList.Union(ret).ToList();
        }
    }
}
