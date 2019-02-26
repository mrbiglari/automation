using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernateDemoApp
{


    public class ProgramNode
    {
        public List<BoolExpr> clauses;
        public string componentName;
        public int index;

        public ProgramNode(string componentName, int index, List<BoolExpr> clauses)
        {
            this.clauses = clauses;
            this.componentName = componentName;
            this.index = index;
        }

    }

    public class ExampleNode
    {
        public List<BoolExpr> clauses;
        public ExampleNode(List<BoolExpr> clauses)
        {
            this.clauses = clauses;
        }
    }

    public class SMTModel
    {
        public List<ProgramNode> satEncodedProgram;
        public List<ExampleNode> satEncodedProgramSpec;
    }
    public static class SMTSolver
    {
        public static Solver InitializeSolver(Context context)
        {
            return context.MkSolver();
        }

        public static List<BoolExpr> FlattenExpressionsList(BoolExpr expression, List<BoolExpr> returnArray = null)
        {
            if (returnArray == null)
                returnArray = new List<BoolExpr>();
            var s = expression.Args.First().Args.Count();
            if ((expression.Args.Count() != 2 || expression.Args.First().Args.Count() != 0) && expression.IsOr == false)
            {
                foreach (var arg in expression.Args)
                {

                    FlattenExpressionsList((BoolExpr)arg, returnArray);
                }
                return returnArray;
            }
            returnArray.Add(expression);

            return returnArray;

        }

        public static List<BoolExpr> SMTSolve(Context context, SMTModel model)
        {
            var satEncodedProgramSpecInstance = model.satEncodedProgramSpec.FirstOrDefault();

            var solver = InitializeSolver(context);

            foreach(var programNode in model.satEncodedProgram)
            {
                foreach(var clause in programNode.clauses)
                {
                    //BoolExpr p = context.MkEq(clause, context.MkBoolConst($"{programNode.index}_{programNode.componentName}"));
                    BoolExpr p = context.MkBoolConst($"{programNode.index}_{programNode.componentName}_{clause.ToString()}");
                    solver.AssertAndTrack(clause,p);
                }
            }


            var example = model.satEncodedProgramSpec.First();
            //foreach(var example in model.satEncodedProgramSpec)
            //{
                foreach(var clause in example.clauses)
                {
                solver.AssertAndTrack(clause, clause);
                }

            //}

            //var expression = context.MkAnd(model.satEncodedProgram, satEncodedProgramSpecInstance);
            
            //var expression = default(BoolExpr);

            //This step is neccessary to make each expression trackable by Z3, otherwise the unsatcore will track to each expression
            //var flattenedExpressionsArray = FlattenExpressionsList(expression).ToArray();
            //var s1 = flattenedExpressionsArray.GroupBy(x => x).Where(g => g.Count() > 1).SelectMany(r => r).ToList();
            //var s = flattenedExpressionsArray.GroupBy(x => x).Select(x => x.First()).ToList();
            //solver.AssertAndTrack(expression, expression);

            //foreach (var expression1 in flattenedExpressionsArray)
            //{
            //    try
            //    {
            //        solver.AssertAndTrack(expression, expression);
            //    }
            //    catch (Exception ex)
            //    {
            //        ;
            //    }
            //}

            //solver = InitializeSolver(context);

            //for (var i = 0; i < flattenedExpressionsArray.Count(); i++)
            //{
            //    try
            //    {
            //        BoolExpr p = context.MkEq(flattenedExpressionsArray[i], context.MkBoolConst($"P{i}"));
            //        solver.AssertAndTrack(flattenedExpressionsArray[i], p);
            //    }
            //    catch (Exception ex)
            //    {
            //        ;
            //    }
            //}


            //solver.AssertAndTrack(flattenedExpressionsArray, flattenedExpressionsArray);

            var result = solver.Check();

            if (result == Status.UNSATISFIABLE)
            {
                Console.WriteLine("unsat");
                Console.WriteLine("core: ");
                foreach (Expr c in solver.UnsatCore)
                {
                    Console.WriteLine("{0}", c);
                }
            }
            if (result == Status.SATISFIABLE)
            {
                Console.WriteLine("sat");
            }

            return solver.UnsatCore.ToList();
        }
    }
}
