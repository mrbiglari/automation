using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Automation
{
    public class GrammarBuilder
    {
        private const string _ruleSeparator = ",";
        private const string _ruleInference = "-->";
        private const string _blankSpace = " ";

        private const string key_startSymbol = "startSymbol";
        private const string key_types = "types";
        private const string key_terminals = "terminals";
        private const string key_nonTerminals = "nonTerminals";
        private const string key_rules = "rule";
        private int maxArity;

        public static Grammar Build(string fileName, List<TypeSpec> typeSpec, Random rand, List<Parameter> parameters)
        {
            var specContent = GetGrammarSpecFile(fileName);
            return BuildGrammarFromSpec(specContent, typeSpec, rand, parameters);
        }

        private static XElement GetGrammarSpecFile(string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var grammarSpecFilepath = Path.Combine(currentDirectory, fileName);
            return XElement.Load(grammarSpecFilepath);
        }

        private static Grammar BuildGrammarFromSpec(XElement grammarSpec, List<TypeSpec> typeSpecs, Random rand, List<Parameter> parameters)
        {
            var countArity = 0;

            var typeConstants = grammarSpec.Descendants(key_types).First().Value.Trim().SplitBy(Symbols.seperator)
                .Select((x, index) =>
                    {
                        return new Parameter(ParameterType.Other,
                            EnumHelper.ToEnum<ArgType>(x.SplitBy("(").First()), x.SplitBy("(").Last().Remove(")").Remove("[").Remove("]"), index + 1);
                    })
                .Select(x =>
                    {
                        var typeSpec = typeSpecs.Where(y => y.type == x.argType).First();
                        var symbol = (x.argType == ArgType.List) ? $"[{x.obj.ToString()}]" : x.obj.ToString();
                        return Tuple.Create(symbol, ProgramSpecBuilder.GetParamByType(x, typeSpec));
                    }).ToList();

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

                    if (rhs.Contains("("))
                    {
                        var rules = parameters.Where(x => x.argType == EnumHelper.ToEnum<ArgType>(rhs.SplitBy("(").First()) && x.parameterType == ParameterType.Input)
                            .Select(x => new Production(leftHandsideSymbol, new List<string>() { x.obj.ToString() }, 0)).ToList();
                        productions.AddRange(rules);
                    }
                    else
                    {
                        var splittedRHS1 = rhs.SplitBy(_blankSpace).ToList().Where(x => nonTerminals.Contains(x)).ToList();
                        if (splittedRHS1.Count() > countArity)
                            countArity = splittedRHS1.Count();

                        var splittedRHS = rhs.SplitBy(_blankSpace);

                        productions.Add(new Production(leftHandsideSymbol, splittedRHS, splittedRHS1.Count()));
                    }

                }

            }
            return new Grammar(startSymbol, nonTerminals, terminals, productions, countArity, typeConstants, rand);
        }
    }
}
