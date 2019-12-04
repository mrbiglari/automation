using Synthesis;
using Synthesizer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesizer.Logic
{

    public delegate Func<string, string, bool> TwoPlace_Predicate();

    class TwoPlace_PredicateClass
    {
        public Term firstArg;
        public Term secondArg;
        public TwoPlace_Predicate predicate;
        public ERelationalOperators opr;
    }
    class Variable<T>
    {
        string name;
    }
    //" (x>y) \and (z>y) \and ((x>u) \or (u>x))"
    class Term
    {
        public string variable;
        public string constant;
        public TwoPlace_PredicateClass predicateSigniture;
    }

    class Formula
    {
        public Term term;
        public LogicallyConnectedFormulas logicallyConnectedFormulas;
    }
    class LogicallyConnectedFormulas
    {
        public List<Formula> formulae;
        public ELogicalOperators opr;
    }



}

namespace Synthesizer.Test
{
    class AtomicFormula
    {
        public Term term;
    }

    class Literal
    {

    }
    class Clause
    {
        public List<Clause> clauses;
        public ELogicalOperators opr;
        public Clause()
        {

        }
    }
    class Formula1
    {
        public List<Clause1> clauses;
        public ELogicalOperators opr;
    }

    class Clause1
    {
        List<Term1> clauses;

    }

    class Term1
    {
        public string variable;
        public TwoPlace_PredicateClass predicate;
    }
}


