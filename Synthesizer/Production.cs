
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NHibernateDemoApp
{

    public class Production
    {
        public string leftHandSide;
        public List<string> rightHandSide;
        public int arity;

        public Production(string lhs, List<string> rhs, int arity)
        {
            leftHandSide = lhs;
            rightHandSide = rhs;
            this.arity = arity;
        }

    }
}