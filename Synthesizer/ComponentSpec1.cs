using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernateDemoApp
{
    public class ComponentSpec1
    {      
        public void UnsatCoreAndProofExample(Context ctx)
        {
            Console.WriteLine("UnsatCoreAndProofExample");

            Solver solver = ctx.MkSolver();

            BoolExpr pa = ctx.MkBoolConst("PredA");
            BoolExpr pb = ctx.MkBoolConst("PredB");
            BoolExpr pc = ctx.MkBoolConst("PredC");
            BoolExpr pd = ctx.MkBoolConst("PredD");
            BoolExpr p1 = ctx.MkBoolConst("P1");
            BoolExpr p2 = ctx.MkBoolConst("P2");
            BoolExpr p3 = ctx.MkBoolConst("P3");
            BoolExpr p4 = ctx.MkBoolConst("P4");
            BoolExpr t = ctx.MkBool(true);
            //BoolExpr[] assumptions = new BoolExpr[] { ctx.MkNot(p1), ctx.MkNot(p2), ctx.MkNot(p3), ctx.MkNot(p4) };
            //var assumptions = new List<BoolExpr> { ctx.MkNot(p1), ctx.MkNot(p2), ctx.MkNot(p3), ctx.MkNot(p4) };
            var assumptions = new List<BoolExpr> { ctx.MkNot(p2) };
            BoolExpr f1 = ctx.MkAnd(new BoolExpr[] { pa, pb, pc });
            BoolExpr f2 = ctx.MkAnd(new BoolExpr[] { pa, ctx.MkNot(pb), pc });
            BoolExpr f3 = ctx.MkOr(ctx.MkNot(pa), ctx.MkNot(pc));
            BoolExpr f4 = pd;

            solver.AssertAndTrack(ctx.MkAnd(f1, p1), ctx.MkAnd(f1, p1));
            solver.AssertAndTrack(ctx.MkOr(f2, p2), ctx.MkOr(f2, p2));
            solver.AssertAndTrack(ctx.MkOr(f3, p3), ctx.MkOr(f3, p3));
            solver.AssertAndTrack(ctx.MkOr(f4, p4), ctx.MkOr(f4, p4));

            //var statement = ctx.MkAnd(p1, ctx.MkNot(p1));
            //solver.AssertAndTrack(statement, statement);
            //solver.AssertAndTrack(ctx.MkAnd(p1, ctx.MkNot(p1)), p1);
            //solver.AssertAndTrack(ctx.MkAnd(p1, ctx.MkNot(p1)), ctx.MkAnd(p1, ctx.MkNot(p1)));
            //solver.Assert(ctx.MkAnd(p1, ctx.MkNot(p1)));
            Status result = solver.Check();

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

        }

        public void test(Context ctx)
        {

            Expr x = ctx.MkConst("x", ctx.MkIntSort());
            Expr y = ctx.MkConst("y", ctx.MkIntSort());
            Expr zero = ctx.MkNumeral(0, ctx.MkIntSort());
            Expr one = ctx.MkNumeral(1, ctx.MkIntSort());
            Expr minusOne = ctx.MkNumeral(-1, ctx.MkIntSort());
            Expr five = ctx.MkNumeral(5, ctx.MkIntSort());
            Expr ten = ctx.MkNumeral(10, ctx.MkIntSort());

            Solver solver = ctx.MkSolver();

            // x > 0
            var s1 = ctx.MkGt((ArithExpr)x, (ArithExpr)zero);
            // y < 5
            var s2 = ctx.MkLt((ArithExpr)y, (ArithExpr)five);
            // x > -1
            var s3 = ctx.MkGt((ArithExpr)x, (ArithExpr)minusOne);
            // y = x + 10
            var s4 = ctx.MkEq((ArithExpr)y, ctx.MkAdd((ArithExpr)x, (ArithExpr)ten));

            var s5 = ctx.MkAnd(new BoolExpr[] { s1, s2, s3 , s4});

            solver.AssertAndTrack(s5, s5);

            //solver.AssertAndTrack(s1, s1);
            //solver.AssertAndTrack(s2, s2);
            //solver.AssertAndTrack(s3, s3);
            //solver.AssertAndTrack(s4, s4);


            Status result = solver.Check();

            if (result == Status.UNSATISFIABLE)
            {
                Console.WriteLine("unsat");
                Console.WriteLine("proof: {0}", solver.Proof);
                Console.WriteLine("core: ");
                foreach (Expr c in solver.UnsatCore)
                {
                    Console.WriteLine("{0}", c);
                }
            }
        }

        public void test1(Context ctx)
        {

            Expr x = ctx.MkConst("x", ctx.MkIntSort());
            Expr y = ctx.MkConst("y", ctx.MkIntSort());
            Expr zero = ctx.MkNumeral(0, ctx.MkIntSort());
            Expr one = ctx.MkNumeral(1, ctx.MkIntSort());
            Expr minusOne = ctx.MkNumeral(-2, ctx.MkIntSort());
            Expr five = ctx.MkNumeral(5, ctx.MkIntSort());
            Expr ten = ctx.MkNumeral(4, ctx.MkIntSort());

            Solver solver = ctx.MkSolver();

            solver.AssertAndTrack(ctx.MkGt((ArithExpr)x, (ArithExpr)zero), ctx.MkGt((ArithExpr)x, (ArithExpr)zero)); // x > 0
            solver.AssertAndTrack(ctx.MkLt((ArithExpr)y, (ArithExpr)five), ctx.MkLt((ArithExpr)y, (ArithExpr)five)); // y < 5
            //s.AssertAndTrack(ctx.MkLt((ArithExpr)x, (ArithExpr)zero), ctx.MkLt((ArithExpr)x, (ArithExpr)zero)); // x < 0
            solver.AssertAndTrack(ctx.MkGt((ArithExpr)x, (ArithExpr)minusOne), ctx.MkGt((ArithExpr)x, (ArithExpr)minusOne));
            solver.AssertAndTrack(ctx.MkEq((ArithExpr)y, ctx.MkAdd((ArithExpr)x, (ArithExpr)ten)), ctx.MkEq((ArithExpr)y, ctx.MkAdd((ArithExpr)x, (ArithExpr)ten))); // y = x + 10
            //solver.AssertAndTrack(ctx.MkEq((ArithExpr)y, (ArithExpr)five), ctx.MkEq((ArithExpr)y, (ArithExpr)five)); // y = 5

            Status result = solver.Check();

            if (result == Status.UNSATISFIABLE)
            {
                Console.WriteLine("unsat");
                Console.WriteLine("proof: {0}", solver.Proof);
                Console.WriteLine("core: ");
                foreach (Expr c in solver.UnsatCore)
                {
                    Console.WriteLine("{0}", c);
                }
            }
        }

        public BoolExpr sort(string x, string y, Context ctx)
        {
            var x_size = (ArithExpr)ctx.MkConst("x.size", ctx.MkIntSort());
            var y_size = (ArithExpr)ctx.MkConst("y.size", ctx.MkIntSort());

            var x_max = (ArithExpr)ctx.MkConst("x.max", ctx.MkIntSort());
            var y_max = (ArithExpr)ctx.MkConst("y.max", ctx.MkIntSort());

            var x_min = (ArithExpr)ctx.MkConst("x.min", ctx.MkIntSort());
            var y_min = (ArithExpr)ctx.MkConst("y.min", ctx.MkIntSort());

            var one = (ArithExpr)ctx.MkNumeral(1, ctx.MkIntSort());

            // y.size == x.size
            var y_size_Eq_x_size = ctx.MkEq(y_size, x_size);

            // y.max == x.max
            var y_max_Eq_x_max = ctx.MkEq(y_max, x_max);

            // y.min == x.min
            var y_min_Eq_x_min = ctx.MkEq(y_min, x_min);

            // y.size > 1
            var y_size_Gt_one = ctx.MkGt(y_size, one);

            // x.size > 1
            var x_size_Gt_one = ctx.MkGt(x_size, one);

            var sort_spec = ctx.MkAnd(new BoolExpr[] 
                {
                    y_size_Eq_x_size,
                    y_max_Eq_x_max,
                    y_min_Eq_x_min,
                    y_size_Gt_one,
                    x_size_Gt_one                   
                });

            return sort_spec;
        }

        public void sort<T>(List<T> inputList, Context ctx)
        {
            var outputList = default(List<T>);

            Expr x_size = ctx.MkConst("x.size", ctx.MkIntSort());
            Expr y_size = ctx.MkConst("y.size", ctx.MkIntSort());

            Expr x_max = ctx.MkConst("x.max", ctx.MkIntSort());
            Expr y_max = ctx.MkConst("y.max", ctx.MkIntSort());

            Expr x = ctx.MkConst("x", ctx.MkIntSort());
            Expr y = ctx.MkConst("y", ctx.MkIntSort());


            Expr zero = ctx.MkNumeral(0, ctx.MkIntSort());
            Expr one = ctx.MkNumeral(1, ctx.MkIntSort());
            Expr minusOne = ctx.MkNumeral(-2, ctx.MkIntSort());
            Expr five = ctx.MkNumeral(5, ctx.MkIntSort());
            Expr ten = ctx.MkNumeral(4, ctx.MkIntSort());



            Solver solver = ctx.MkSolver();

            solver.AssertAndTrack(ctx.MkGt((ArithExpr)x, (ArithExpr)zero), ctx.MkGt((ArithExpr)x, (ArithExpr)zero)); // x > 0
            solver.AssertAndTrack(ctx.MkLt((ArithExpr)y, (ArithExpr)five), ctx.MkLt((ArithExpr)y, (ArithExpr)five)); // y < 5
            //s.AssertAndTrack(ctx.MkLt((ArithExpr)x, (ArithExpr)zero), ctx.MkLt((ArithExpr)x, (ArithExpr)zero)); // x < 0
            solver.AssertAndTrack(ctx.MkGt((ArithExpr)x, (ArithExpr)minusOne), ctx.MkGt((ArithExpr)x, (ArithExpr)minusOne));
            solver.AssertAndTrack(ctx.MkEq((ArithExpr)y, ctx.MkAdd((ArithExpr)x, (ArithExpr)ten)), ctx.MkEq((ArithExpr)y, ctx.MkAdd((ArithExpr)x, (ArithExpr)ten))); // y = x + 10

            Status result = solver.Check();

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
        }

        public void sorts(Context ctx)
        {
            Expr x = ctx.MkConst("x", ctx.MkIntSort());
            Expr y = ctx.MkConst("y", ctx.MkIntSort());
            Expr z = ctx.MkConst("z", ctx.MkIntSort());


            Expr zero = ctx.MkNumeral(0, ctx.MkIntSort());
            Expr one = ctx.MkNumeral(1, ctx.MkIntSort());
            Expr minusOne = ctx.MkNumeral(-2, ctx.MkIntSort());
            Expr five = ctx.MkNumeral(5, ctx.MkIntSort());
            Expr ten = ctx.MkNumeral(4, ctx.MkIntSort());

            var assumptions = new List<BoolExpr> { ctx.MkEq(z, one) };
            Solver solver = ctx.MkSolver();

            solver.AssertAndTrack(ctx.MkGt((ArithExpr)x, (ArithExpr)zero), ctx.MkGt((ArithExpr)x, (ArithExpr)zero));

            solver.AssertAndTrack(ctx.MkGt((ArithExpr)x, (ArithExpr)z), ctx.MkGt((ArithExpr)x, (ArithExpr)z)); // x > z
            //solver.AssertAndTrack(ctx.MkEq((ArithExpr)z, (ArithExpr)one), ctx.MkEq((ArithExpr)z, (ArithExpr)one)); // z == 1
            solver.AssertAndTrack(ctx.MkLt((ArithExpr)y, (ArithExpr)five), ctx.MkLt((ArithExpr)y, (ArithExpr)five)); // y < 5
            //s.AssertAndTrack(ctx.MkLt((ArithExpr)x, (ArithExpr)zero), ctx.MkLt((ArithExpr)x, (ArithExpr)zero)); // x < 0
            solver.AssertAndTrack(ctx.MkGt((ArithExpr)x, (ArithExpr)minusOne), ctx.MkGt((ArithExpr)x, (ArithExpr)minusOne));
            solver.AssertAndTrack(ctx.MkEq((ArithExpr)y, ctx.MkAdd((ArithExpr)x, (ArithExpr)ten)), ctx.MkEq((ArithExpr)y, ctx.MkAdd((ArithExpr)x, (ArithExpr)ten))); // y = x + 10

            Status result = solver.Check(assumptions);
            //Status result = solver.Check();

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
        }
    }
}
