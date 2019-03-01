using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis
{
    public class ComponentSpec
    {
        public string name;
        public BoolExpr spec;
        
        public ComponentSpec(string name, BoolExpr spec)
        {
            this.name = name;
            this.spec = spec;
        }

    }
}
