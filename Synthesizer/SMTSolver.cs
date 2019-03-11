using Microsoft.Z3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis
{

    public class UnSatCores : List<UnSatCore>
    {
        public UnSatCores()
        {
        }
        public UnSatCores(IEnumerable<UnSatCore> lemmas) : base(lemmas)
        {
        }

        public Boolean IsUnSAT(Context context)
        {
            var unSATCoresInConjuction = context.MkAnd
                (
                    this.SelectMany(x => x).
                    Select(x => context.MkNot(x.spec))
                );
            return SMTSolver.CheckIfUnSAT(context, unSATCoresInConjuction);
        }
    }

    public class UnSatCore : List<UnSatCoreClause>
    {
        public UnSatCore()
        {
        }
        public UnSatCore(IEnumerable<UnSatCoreClause> lemma) : base(lemma)
        {
        }
    }

    public class UnSatCoreClause
    {
        public string name;
        public string index;
        public BoolExpr spec;

        public UnSatCoreClause(string name, string index, BoolExpr spec)
        {
            this.name = name;
            this.index = index;
            this.spec = spec;
        }
    }
    public class Pair<T, U>
    {
        public Pair()
        {
        }

        public Pair(T first, U second)
        {
            this.First = first;
            this.Second = second;
        }

        public T First { get; set; }
        public U Second { get; set; }
    };

    public class ProgramNode
    {
        public Pair<List<BoolExpr>, List<BoolExpr>> clauses;
        public string componentName;
        public int index;

        public ProgramNode(string componentName, int index, Pair<List<BoolExpr>, List<BoolExpr>> clauses)
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

        public static bool CheckIfUnSAT(Context context, BoolExpr check)
        {
            var solver = InitializeSolver(context);
            solver.AssertAndTrack(check, check);
            var result = solver.Check();
            return (result == Status.UNSATISFIABLE);
        }

            public static UnSatCore SMTSolve(Context context, SMTModel model)
        {
            var satEncodedProgramSpecInstance = model.satEncodedProgramSpec.FirstOrDefault();

            var solver = InitializeSolver(context);

            foreach (var programNode in model.satEncodedProgram)
            {
                foreach (var clause in programNode.clauses.First)
                {
                    BoolExpr p = context.MkAnd(
                        context.MkBoolConst($"{programNode.index}_{programNode.componentName}"),
                        clause);
                    solver.AssertAndTrack(clause, p);
                }
            }


            var example = model.satEncodedProgramSpec.First();

            foreach (var clause in example.clauses)
            {
                solver.AssertAndTrack(clause, clause);
            }


            var result = solver.Check();

            if (result == Status.UNSATISFIABLE)
            {

                var UNSATCoreExcludedProgramSpec = solver.UnsatCore.Where(x => !example.clauses.Contains(x)).ToList();
                var unSATCore = UNSATCoreExcludedProgramSpec.Select(x =>
               {

                   var splitted = x.Args[0].ToString().Replace("|", "").SplitBy("_");
                   var name = splitted[1];
                   var index = splitted[0];
                   var expression = (BoolExpr)x.Args[1];
                   var temp = model.satEncodedProgram.Where(y => y.componentName == name).Where(y => y.clauses.First.Contains(expression)).First();
                   var expressionOriginal = temp.clauses.Second?.ElementAt(temp.clauses.First.IndexOf(expression)) ?? default(BoolExpr);

                   return new UnSatCoreClause(name, index, expressionOriginal);
               }
                ).OrderBy(x => Int32.Parse(x.index)).ToList().AsUnSATCore();

                Console.WriteLine("unsat");
                Console.WriteLine("core: ");
                foreach (Expr c in solver.UnsatCore)
                {
                    Console.WriteLine("{0}", c);
                }
                return unSATCore;
            }
            if (result == Status.SATISFIABLE)
            {
                Console.WriteLine("sat");
            }
            return null;
        }
    }
}
