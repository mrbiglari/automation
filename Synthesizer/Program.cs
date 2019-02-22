using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Z3;
namespace NHibernateDemoApp
{   
    public class Program
    {
        public const string specsFolderPath = "Specs/";
        static void Main(string[] args)
        {

            var path_grammarSpec = specsFolderPath + "GrammarSpec.xml";
            var path_componentSpec = specsFolderPath + "ComponentSpecs.xml";
            var path_programSpec = specsFolderPath + "ProgramSpec.xml";

            var z3ComponentsSpecs = new List<Tuple<string, string>>();
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {
                //var component = new ComponentSpec1();
                //component.test(ctx);

                z3ComponentsSpecs = ComponentSpecsBuilder.Build(path_componentSpec, context);
                var programSpec = ProgramSpecBuilder.Build(path_programSpec, context);
                var grammar = GrammarBuilder.Build(path_grammarSpec);

                var counter = 1;
                while (true)
                {
                    if (counter % 100 == 0)
                        Console.ReadLine();
                    var program = grammar.generateRandomProgram();

                    if (program.ElementsIndex.Count() > 7)
                    {
                        //Console.ReadLine();
                        program = program.ChipRoot();
                        var satEncodedArtifactsAsSMTModel = SATEncoder<string>.SATEncode(z3ComponentsSpecs, context, programSpec, program);
                        var unsatCore = SMTSolver.SMTSolve(context, satEncodedArtifactsAsSMTModel);
                        //var satEncodedProgramArgs = satEncodedProgram.Args;
                    }                   
                    
                    program.Visualize();

                    counter++;
                }
            }
        }
    }
}