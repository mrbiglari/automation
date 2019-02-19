using Microsoft.Z3;
using NHibernateDemoApp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CSharpTree
{
    public class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        public T Data { get; set; }
        public string Spec;
        public TreeNode<T> Parent { get; set; }
        public ICollection<TreeNode<T>> Children { get; set; }
        public double index;
        public int arity;

        public Boolean IsRoot
        {
            get { return Parent == null; }
        }

        public Boolean IsLeaf
        {
            get { return Children.Count == 0; }
        }

        public int Level
        {
            get
            {
                if (this.IsRoot)
                    return 0;
                return Parent.Level + 1;
            }
        }

        public TreeNode(T data)
        {
            this.Data = data;
            this.Children = new LinkedList<TreeNode<T>>();

            this.ElementsIndex = new LinkedList<TreeNode<T>>();
            this.ElementsIndex.Add(this);
        }
        public TreeNode()
        {
            this.Children = new LinkedList<TreeNode<T>>();

            this.ElementsIndex = new LinkedList<TreeNode<T>>();
            this.ElementsIndex.Add(this);
        }

        public TreeNode<T> AddChild(T child, int arity)
        {
            TreeNode<T> childNode = new TreeNode<T>(child) { Parent = this, arity = arity };
            this.Children.Add(childNode);

            this.RegisterChildForSearch(childNode);

            return childNode;
        }
        public TreeNode<T> AddChild()
        {
            TreeNode<T> childNode = new TreeNode<T>() { Parent = this };
            this.Children.Add(childNode);

            this.RegisterChildForSearch(childNode);

            return childNode;
        }

        public void RemoveElement(TreeNode<T> root, T data)
        {
            var elementToDelete = root.Where(x => Comparer<T>.Default.Compare(x.Data, data) >= 0).FirstOrDefault();

            elementToDelete.Parent.Children = elementToDelete.Children;
            foreach(var element in elementToDelete.Children)
            {
                element.Parent = elementToDelete.Parent;
            }           
        }

        public BoolExpr SATEncode(List<Tuple<string, string>> componentSpecs, Context context, ProgramSpec programSpec)
        {
            var programSpecAsString = SATEncoder<T>.GetProgramSpecZ3AsString(programSpec);
            var programSpecAsZ3Expression = SATEncoder<T>.GetProgramSpecZ3Expression(programSpecAsString, context);
            var satEncodingList = SATEncoder<T>.SATEncode(this, componentSpecs, context);
            //var satEncoding = context.MkAnd(satEncodingList.Select(x => x.spec).ToArray());

            var satEncodings = SATEncoder<T>.GenerateZ3Expression(this, context, programSpec);
            var satEncoding = context.MkAnd(satEncodings.Select(x => x.spec).ToArray());
            //var satEncoding = context.MkAnd(satEncodingList.ToArray());
            return satEncoding;
        }

        public TreeNode<T> ChipRoot()
        {
            this.Children.First().Parent = null;
            return this.Children.First();
        }

        public void Visualize()
        {
            TreeVisualizer<T>.PrintNodes(this, "");
        }

        public override string ToString()
        {
            return Data != null ? Data.ToString() : "[data null]";
        }

        #region searching

        public ICollection<TreeNode<T>> ElementsIndex { get; set; }

        private void RegisterChildForSearch(TreeNode<T> node)
        {
            ElementsIndex.Add(node);
            if (Parent != null)
                Parent.RegisterChildForSearch(node);
        }

        public TreeNode<T> FindTreeNode(Func<TreeNode<T>, bool> predicate)
        {
            return this.ElementsIndex.FirstOrDefault(predicate);
        }

        #endregion

        #region iterating

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            yield return this;
            foreach (var directChild in this.Children)
            {
                foreach (var anyChild in directChild)
                    yield return anyChild;
            }
        }

        #endregion
    }
}