//using CSharpTree;
//using Microsoft.Z3;
//using Synthesis;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Synthesizer
//{
//    public class LemmaHelper<T>
//    {
//        public static void asd(UnSatCore unSATCore, TreeNode<T> programRoot, Context context, Grammar grammar, List<Tuple<string, string> > z3ComponentsSpecs)
//        {
//            if (unSATCore?.Count != 0)
//            {
//                unSATCores.Add(unSATCore);

//                foreach (var clause in unSATCore)
//                {
//                    // var lemma = new Lemma();
//                    var rule = programRoot.GetAtIndex(Int32.Parse(clause.index)).rule;
//                    var componentsToCheck = grammar.productions.Where(x => x.leftHandSide == rule.leftHandSide && x.rightHandSide.First() != clause.name)
//                        .Select(x => x.rightHandSide.First()).ToList();

//                    var lemmaClause = new LemmaClause();

//                    var lemmaSub = unSATCore.Where(x => x != clause).Select(x =>
//                    {
//                        lemmaClause = new LemmaClause();
//                        lemmaClause.Add(context.MkNot(x.expression));
//                        return lemmaClause;
//                    }).AsLemma();

//                    lemmaClause = new LemmaClause();
//                    lemmaClause.Add(context.MkNot(clause.expression));

//                    if (clause.spec != null)
//                    {
//                        foreach (var component in componentsToCheck)
//                        {

//                            var componentSpec = z3ComponentsSpecs.Where(x => x.Item1 == component).FirstOrDefault();
//                            if (componentSpec != null)
//                            {
//                                var z3ComponentSpec = context.MkAnd(ComponentSpecsBuilder.GetComponentSpec(componentSpec));

//                                var check = context.MkNot(context.MkImplies(z3ComponentSpec, clause.spec));
//                                var lightEncoding = unSATCore.Where(x => x != clause);
//                                if (SMTSolver.CheckIfUnSAT(context, check))
//                                {
//                                    lemmaClause.Add
//                                        (
//                                            context.MkNot
//                                            (
//                                                context.MkBoolConst($"C_{clause.index}_{component}")
//                                            )
//                                        );
//                                }
//                            }

//                        }
//                    }
//                    lemmaSub.Add(lemmaClause);
//                    lemmaSub.lemmaLength = unSATCores.SelectMany(x => x).Max(x => x.index).ToInt();
//                    lemmas.Add(lemmaSub);
//                }

//                if (currentNode.Parent != null)
//                {
//                    currentNode.Parent.Children.Remove(currentNode);
//                    currentNode = currentNode.Parent;
//                }
//                else
//                {
//                    programRoot = new TreeNode<string>();
//                    currentNode = programRoot;
//                }
//            }
//        }
//    }
//}
