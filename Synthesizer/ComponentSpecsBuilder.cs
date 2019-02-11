using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NHibernateDemoApp
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
        public static BoolExpr GetSpec(ERelationalOperators opr, ArithExpr x, ArithExpr y, Context ctx)
        {
            switch (opr)
            {
                case (ERelationalOperators.Eq):
                    return ctx.MkEq(x,y);

                case (ERelationalOperators.Gt):
                    return ctx.MkGt(x, y);

                case (ERelationalOperators.L):
                    return ctx.MkLt(x, y);

                case (ERelationalOperators.GtEq):
                    var first = ctx.MkEq(x, y);
                    var second = ctx.MkGt(x, y);
                    return ctx.MkOr(first, second);

                case (ERelationalOperators.LEq):
                    var firsts = ctx.MkEq(x, y);
                    var seconds = ctx.MkLt(x, y);
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

        public static BoolExpr GetSpec(ELogicalOperators opr, BoolExpr x, BoolExpr y, Context ctx)
        {
            switch (opr)
            {
                case (ELogicalOperators.AND):
                    return ctx.MkAnd(new BoolExpr[]{x, y});

                case (ELogicalOperators.OR):
                    return ctx.MkOr(new BoolExpr[] { x, y });

                case (ELogicalOperators.NOT):
                    return ctx.MkNot(x);

                case (ELogicalOperators.EQV):
                    return ctx.MkEq(x, y);
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

        public static ArithExpr GetSpec(EArithmaticOperators opr, ArithExpr x, ArithExpr y, Context ctx)
        {
            switch (opr)
            {
                case (EArithmaticOperators.SUM):
                    return ctx.MkAdd(x, y);

                case (EArithmaticOperators.SUB):
                    return ctx.MkSub(x, y);

                case (EArithmaticOperators.MUL):
                    return ctx.MkMul(x, y);

                case (EArithmaticOperators.DIV):
                    return ctx.MkDiv(x, y);
            }
            return null;
        }
    }

    public class ComponentSpecsBuilder
    {
        public const string key_componentSpec= "ComponentSpec";
        public const string key_name = "Name";
        public const string key_spec = "Spec";
        public static Context context;

        public static List<Tuple<string, string>> Build(string fileName, Context ctx)
        {
            context = ctx;
            var specContent = GetComponentSpecsFile(fileName);
            return BuildComponentSpecsFromSpec(specContent);
        }

        private static XElement GetComponentSpecsFile(string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var componentSpecsFilepath = Path.Combine(currentDirectory, fileName);
            return XElement.Load(componentSpecsFilepath);
        }

        private static ArithExpr GetArg(string term)
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


        public static ComponentSpec GetComponentSpec(Tuple<string, string> componentSpec)
        {
            var name = componentSpec.Item1;
            var specList = componentSpec.Item2.SplitBy(Operators.GetSymbolFor(ELogicalOperators.AND)).Select(x => x.Trim()).ToList();

            var z3SpecsList = new List<BoolExpr>();
            foreach (var spec in specList)
            {
                var opr = spec.ContainsWhich(RelationalOperators.operators);
                var operands = spec.SplitBy(RelationalOperators.operators[opr]);

                var arg_1 = GetArg(operands.First());
                
                var arg_2 = GetArg(operands.Last());

                var boolExpr = RelationalOperators.GetSpec(opr, arg_1, arg_2, context);
                z3SpecsList.Add(boolExpr);
            }
            var z3Spec = (z3SpecsList.Count > 1) ? context.MkAnd(z3SpecsList.ToArray()) : z3SpecsList.First();

            return new ComponentSpec(name, z3Spec);
        }

        private static List<Tuple<string, string>> BuildComponentSpecsFromSpec(XElement componentSpecsXML)
        {
            var componentSpecsList = componentSpecsXML.Descendants(key_componentSpec)
                .Select(x => 
                    Tuple.Create(
                            x.Descendants(key_name).FirstOrDefault().Value.Trim(),
                            x.Descendants(key_spec).FirstOrDefault().Value.Trim()
                    )
                ).ToList();

            return componentSpecsList;

            //var z3ComponentSpecs = new List<ComponentSpec>();
            //foreach(var componentSpec in componentSpecsList)
            //{
            //    var name = componentSpec.Item1;
            //    var specList = componentSpec.Item2.SplitBy(Operators.GetSymbolFor(ELogicalOperators.AND)).Select(x => x.Trim()).ToList();
                
            //    var z3SpecsList = new List<BoolExpr>();
            //    foreach(var spec in specList)
            //    {
            //        var opr = spec.ContainsWhich(RelationalOperators.operators);
            //        var operands = spec.SplitBy(RelationalOperators.operators[opr]);

            //        var arg_1 = GetArg(operands.First());
            //        var arg_2 = GetArg(operands.Last());

            //        var boolExpr = RelationalOperators.GetSpec(opr, arg_1, arg_2, context);
            //        z3SpecsList.Add(boolExpr);
            //    }
            //    var z3Spec = context.MkAnd(z3SpecsList.ToArray());
            //    z3ComponentSpecs.Add(new ComponentSpec(name, z3Spec));
            //}
                        
            //return z3ComponentSpecs;            
        }
    }
}
