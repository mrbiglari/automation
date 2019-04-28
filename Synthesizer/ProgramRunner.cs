using CSharpTree;
using Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesizer
{
    public class ProgramRunner
    {
        public object ExecuteProgram(TreeNode<string> node, object[] programArguments)
        {
            var methodName = node.Data;
            var type = typeof(ComponentConcreteSpecs);
            var method = type.GetMethod(methodName);
            var args = new List<object>();

            foreach (var child in node.Children)
            {
                args.Add(ExecuteProgram(child, programArguments));
            }

            if (node.Children.Count == 0)
            {
                if (node.Data.Contains("x"))
                {
                    return programArguments[Int32.Parse(node.Data.SplitBy("x").Last()) - 1];
                }
                else if (method != null)
                    return method.CreateDelegate(this);
                else
                    return Int32.Parse(node.Data);
            }
            else
                return method.Invoke(null, args.ToArray());


        }
    }
}
