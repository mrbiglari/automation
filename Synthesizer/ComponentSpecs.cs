using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernateDemoApp
{
    public class ComponentSpecs
    {
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

        public BoolExpr head(string x, string y, Context ctx)
        {
            var x_size = (ArithExpr)ctx.MkConst("x.size", ctx.MkIntSort());
            var y_size = (ArithExpr)ctx.MkConst("y.size", ctx.MkIntSort());

            var x_first = (ArithExpr)ctx.MkConst("x.first", ctx.MkIntSort());
            var y_first = (ArithExpr)ctx.MkConst("y.first", ctx.MkIntSort());

            var x_max = (ArithExpr)ctx.MkConst("x.max", ctx.MkIntSort());
            var y_max = (ArithExpr)ctx.MkConst("y.max", ctx.MkIntSort());

            var x_min = (ArithExpr)ctx.MkConst("x.min", ctx.MkIntSort());
            var y_min = (ArithExpr)ctx.MkConst("y.min", ctx.MkIntSort());

            var one = (ArithExpr)ctx.MkNumeral(1, ctx.MkIntSort());


            var sort_spec = ctx.MkAnd(new BoolExpr[]
                {
                });

            return sort_spec;
        }
    }
}
