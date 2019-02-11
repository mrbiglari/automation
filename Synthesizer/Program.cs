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
        static void Main(string[] args)
        {

            var fileName_grammarSpec = "GrammarSpec.xml";
            var fileName_componentSpec = "ComponentSpecs.xml";
            var fileName_programSpec = "ProgramSpec.xml";

            var z3ComponentsSpecs = new List<Tuple<string, string>>();
            using (Context ctx = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {
                //var component = new ComponentSpec1();
                //component.test(ctx);

                z3ComponentsSpecs = ComponentSpecsBuilder.Build(fileName_componentSpec, ctx);
                var programSpec = ProgramSpecBuilder.Build(fileName_programSpec, ctx);
                var grammar = GrammarBuilder.Build(fileName_grammarSpec);

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
                        var satEncodedProgram = program.SATEncode(z3ComponentsSpecs, ctx);
                        var satEncodedProgramArgs = satEncodedProgram.Args;
                    }                   
                    
                    program.Visualize();

                    counter++;
                }
            }
        }
    }
}