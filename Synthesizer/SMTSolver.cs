using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernateDemoApp
{
    public class SMTModel
    {
        public BoolExpr satEncodedProgram;
        public List<BoolExpr> satEncodedProgramSpec;
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

            var expression = context.MkAnd(model.satEncodedProgram, satEncodedProgramSpecInstance);

            //This step is neccessary to make each expression trackable by Z3, otherwise the unsatcore will track to each expression
            var flattenedExpressionsArray = FlattenExpressionsList(expression).ToArray();
            
            //solver.AssertAndTrack(expression, expression);
            solver.AssertAndTrack(flattenedExpressionsArray, flattenedExpressionsArray);

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
