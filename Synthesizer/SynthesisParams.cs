using System.Collections.Generic;

namespace Synthesis
{
    public class SynthesisParams
    {
        public List<TypeSpec> typeSpecs;
        public ProgramSpec programSpec;

        public Grammar grammar;
        public Grammar grammarGround;
        public List<Z3ComponentSpecs> z3ComponentSpecs;
        public int benchmarkId;
    }
}