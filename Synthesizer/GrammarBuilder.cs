using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NHibernateDemoApp
{
    public class GrammarBuilder
    {
        private const string _ruleSeparator = ",";
        private const string _ruleInference = "-->";
        private const string _blankSpace = " ";

        private const string key_startSymbol = "startSymbol";
        private const string key_terminals = "terminals";
        private const string key_nonTerminals = "nonTerminals";
        private const string key_rules = "rule";
        private int maxArity;

        public static Grammar Build(string fileName)
        {
            var specContent = GetGrammarSpecFile(fileName);
            return BuildGrammarFromSpec(specContent);
        }

        private static XElement GetGrammarSpecFile(string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var grammarSpecFilepath = Path.Combine(currentDirectory, fileName);
            return XElement.Load(grammarSpecFilepath);
        }

        private static Grammar BuildGrammarFromSpec(XElement grammarSpec)
        {
            var countArity = 0;
            var startSymbol = grammarSpec.Descendants(key_startSymbol)
                    .Select(x => x.Value.TrimStart().TrimEnd()).FirstOrDefault();

            var terminals = grammarSpec.Descendants(key_terminals)
                    .Select(x => x.Value.TrimStart().TrimEnd()).FirstOrDefault()
                        .SplitBy(_ruleSeparator);

            var nonTerminals = grammarSpec.Descendants(key_nonTerminals)
                    .Select(x => x.Value.TrimStart().TrimEnd()).FirstOrDefault()
                        .SplitBy(_ruleSeparator);

            var productionRuleEntries = grammarSpec.Descendants(key_rules)
                .Select(x => x.Value.TrimStart().TrimEnd()).ToList();

            var productions = new List<Production>();
            foreach (var entry in productionRuleEntries)
            {
                var splitedEntry = entry.SplitBy(_ruleInference);
                var leftHandsideSymbol = splitedEntry.Select(x => x.Trim()).First();
                var rightHandSideSymbols = splitedEntry.Last().SplitBy(_ruleSeparator);                

                foreach (var rhs in rightHandSideSymbols)
                {

                    var splittedRHS1 = rhs.SplitBy(_blankSpace).ToList().Where(x => nonTerminals.Contains(x)).ToList();
                    if (splittedRHS1.Count() > countArity)
                        countArity = splittedRHS1.Count();

                    var splittedRHS = rhs.SplitBy(_blankSpace);

                    productions.Add(new Production(leftHandsideSymbol, splittedRHS, splittedRHS1.Count()));
                }

            }
            return new Grammar(startSymbol, nonTerminals, terminals, productions, countArity);
        }
    }
}
