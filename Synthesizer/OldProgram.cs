using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Synthesis
{

    class OldProgram
    {
        //static void Main(string[] args)
        //{
        //    //var terminals = grammarSpec.Descendants("terminals").Select(x => (string)x.Attribute("PartNumber"));

        //    var temp = RetrieveFromXML();

        //    var grammar = new Grammar();
          
        //    grammar.startSymbol = "N";
        //    grammar.nonTerminals = new List<string>() { "N", "L", "T" };
        //    grammar.terminals = new List<string>() { "0", "10", "head", "sort", "filter", "eqz", "x1", "x2" };

        //    // N --> 0
        //    grammar.addProduction("N", new List<string>{"0"});
        //    grammar.addProduction("N", new List<string> { "x1" });
        //    grammar.addProduction("N", new List<string> { "x2" });
        //    // N --> 10
        //    grammar.addProduction("N", new List<string> { "10"});
        //    // N --> head L
        //    grammar.addProduction("N", new List<string> { "head", "L"});

        //    grammar.addProduction("L", new List<string> { "sort", "L"});
        //    grammar.addProduction("L", new List<string> { "x1" });
        //    grammar.addProduction("L", new List<string> { "x2" });

        //    grammar.addProduction("L", new List<string> { "filter", "L", "T"});
        //    grammar.addProduction("T", new List<string> { "eqz"});

        //    while (true)
        //    {
        //        var program = temp.generateRandomProgram();
        //        program.Visualize();
        //    }
        //}
       
    }
}