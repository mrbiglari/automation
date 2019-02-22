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
        //public Solver solver;
        //public BoolExpr satEncodedProgram;
        //public List<BoolExpr> satEncodedProgramSpec;


        public static Solver InitializeSolver(Context context)
        {
            return context.MkSolver();
        }

        public static List<BoolExpr> SMTSolve(Context context, SMTModel model)
        {
            //satEncodedProgram = model.satEncodedProgram;
            //satEncodedProgramSpec = model.satEncodedProgramSpec;

            var satEncodedProgramSpecInstance = model.satEncodedProgramSpec.FirstOrDefault();

            var solver = InitializeSolver(context);

            var expression = context.MkAnd(model.satEncodedProgram, satEncodedProgramSpecInstance);
            solver.AssertAndTrack(expression, expression);

            var result = solver.Check();

            if (result == Status.UNSATISFIABLE)
            {
                Console.WriteLine("unsat");
                //Console.WriteLine("proof: {0}", solver.Proof);
                Console.WriteLine("core: ");
                foreach (Expr c in solver.UnsatCore)
                {
                    Console.WriteLine("{0}", c);
                }
            }
            if (result == Status.SATISFIABLE)
            {
                Console.WriteLine("sat");
                //Console.WriteLine(String.Join(" ", solver.Units.ToList()));
            }

            return solver.UnsatCore.ToList();
        }
    }
}
