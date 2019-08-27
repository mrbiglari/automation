
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Automation
{
    [Serializable]
    public class Production
    {
        public string leftHandSide;
        public List<string> rightHandSide;
        public int arity;
        public string component
        {
            get { return rightHandSide.First(); }
        }
        public Production(string lhs, List<string> rhs, int arity)
        {
            leftHandSide = lhs;
            rightHandSide = rhs;
            this.arity = arity;
        }

    }
}