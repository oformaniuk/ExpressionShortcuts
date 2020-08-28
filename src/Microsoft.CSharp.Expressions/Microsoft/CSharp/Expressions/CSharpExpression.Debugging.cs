// Prototyping extended expression trees for C#.
//
// bartde - November 2015

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Microsoft.CSharp.Expressions
{
    // DESIGN: I don't like the reference to System.Xml.* assemblies here. Keeping it for now to unblock testing
    //         without having to create a more fancy debug view generator with indent/outdent behavior etc.
    //
    //         The best alternative would be to get extensible DebugView support in LINQ through a mechanism
    //         similar to the one used here in order to dispatch into extension nodes, but likely without using
    //         System.Xml.Linq APIs to reduce the dependency cost.

    partial class CSharpExpression : IDebugViewExpression, ICSharpPrintableExpression
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        public XNode Accept(IDebugViewExpressionVisitor visitor)
        {
            return new CSharpDebugViewExpressionVisitor(visitor).GetDebugView(this);
        }

        internal string DebugView => this.DebugView().ToString();
    }

    partial class Interpolation
    {
        internal string DebugView => new CSharpDebugViewExpressionVisitor().GetDebugView(this).ToString();
    }

    partial class ParameterAssignment
    {
        internal string DebugView => new CSharpDebugViewExpressionVisitor().GetDebugView(this).ToString();
    }

    class CSharpDebugViewExpressionVisitor : CSharpExpressionVisitor
    {
        private readonly IDebugViewExpressionVisitor _parent;
        private readonly Stack<XNode> _nodes = new Stack<XNode>();

        public CSharpDebugViewExpressionVisitor()
            : this(new DebugViewExpressionVisitor())
        {
        }

        public CSharpDebugViewExpressionVisitor(IDebugViewExpressionVisitor parent)
        {
            _parent = parent;
        }

        public XNode GetDebugView(CSharpExpression expression)
        {
            base.Visit(expression);
            return _nodes.Pop();
        }
        
        public XNode GetDebugView(ParameterAssignment assignment)
        {
            return Visit(assignment);
        }

        public XNode GetDebugView(Interpolation interpolation)
        {
            return Visit(interpolation);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitArrayAccess(ArrayAccessCSharpExpression node)
        {
            var args = new List<object>
            {
                new XElement(nameof(node.Array), Visit(node.Array)),
                Visit(nameof(node.Indexes), node.Indexes)
            };

            return Push(node, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitAsyncLambda<TDelegate>(AsyncCSharpExpression<TDelegate> node)
        {
            var parameters = Visit(nameof(AsyncCSharpExpression<TDelegate>.Parameters), node.Parameters);

            var body = Visit(node.Body);

            return Push(node, parameters, new XElement(nameof(AsyncCSharpExpression<TDelegate>.Body), body));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitAwait(AwaitCSharpExpression node)
        {
            var args = new List<object>
            {
                new XElement(nameof(node.Info), Visit(node.Info)),
                new XElement(nameof(node.Operand), Visit(node.Operand))
            };

            return Push(node, args);
        }

        protected internal override AwaitInfo VisitAwaitInfo(StaticAwaitInfo node)
        {
            var args = new List<object>
            {
                new XElement(nameof(node.GetAwaiter), Visit(node.GetAwaiter)),
                new XAttribute(nameof(node.IsCompleted), node.IsCompleted),
                new XAttribute(nameof(node.GetResult), node.GetResult)
            };

            _nodes.Push(new XElement(nameof(StaticAwaitInfo), args));

            return node;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitBlock(BlockCSharpExpression node)
        {
            var args = new List<object>();

            if (node.Variables.Count > 0)
            {
                args.Add(Visit(nameof(node.Variables), node.Variables));
            }

            args.Add(Visit(nameof(node.Statements), node.Statements));

            if (node.ReturnLabel != null)
            {
                args.Add(new XElement(nameof(node.ReturnLabel), _parent.GetDebugView(node.ReturnLabel)));
            }

            return Push(node, args);
        }

        // DESIGN: We can do away with those if we decide to scrap ConditionalAccessCSharpExpression<TExpression> and/or
        //         the specialized conditional node types. We could keep the factories as a convenience to construct the
        //         underyling ConditionalAccess construct.

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitConditionalArrayIndex(ConditionalArrayIndexCSharpExpression node)
        {
            var array = Visit(node.Array);
            var args = Visit(nameof(node.Indexes), node.Indexes);

            return Push("CSharpConditionalArrayIndex", node, new XElement(nameof(node.Array), array), args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitConditionalIndex(ConditionalIndexCSharpExpression node)
        {
            var obj = Visit(node.Object);
            var args = Visit(nameof(node.Arguments), node.Arguments, Visit);

            return Push("CSharpConditionalIndex", node, new XAttribute(nameof(node.Indexer), node.Indexer), new XElement(nameof(node.Object), obj), args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitConditionalInvocation(ConditionalInvocationCSharpExpression node)
        {
            var expr = Visit(node.Expression);
            var args = Visit(nameof(node.Arguments), node.Arguments, Visit);

            return Push("CSharpConditionalInvoke", node, new XElement(nameof(node.Expression), expr), args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitConditionalMember(ConditionalMemberCSharpExpression node)
        {
            var expr = Visit(node.Expression);

            return Push("CSharpConditionalMemberAccess", node, new XAttribute(nameof(node.Member), node.Member), new XElement(nameof(node.Expression), expr));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitConditionalMethodCall(ConditionalMethodCallCSharpExpression node)
        {
            var obj = Visit(node.Object);
            var args = Visit(nameof(node.Arguments), node.Arguments, Visit);

            return Push("CSharpConditionalCall", node, new XAttribute(nameof(node.Method), node.Method), new XElement(nameof(node.Object), obj), args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitDiscard(DiscardCSharpExpression node)
        {
            return Push("CSharpDiscard", node);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitDo(DoCSharpStatement node)
        {
            var args = new List<object>
            {
                new XElement(nameof(node.Body), Visit(node.Body)),
                new XElement(nameof(node.Test), Visit(node.Test))
            };

            if (node.BreakLabel != null)
            {
                args.Add(new XElement(nameof(node.BreakLabel), _parent.GetDebugView(node.BreakLabel)));
            }

            if (node.ContinueLabel != null)
            {
                args.Add(new XElement(nameof(node.ContinueLabel), _parent.GetDebugView(node.ContinueLabel)));
            }

            return Push(node, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitFor(ForCSharpStatement node)
        {
            var args = new List<object>();

            if (node.Variables.Count > 0)
            {
                args.Add(Visit(nameof(node.Variables), node.Variables));
            }

            if (node.Initializers.Count > 0)
            {
                args.Add(Visit(nameof(node.Initializers), node.Initializers));
            }

            if (node.Test != null)
            {
                args.Add(new XElement(nameof(node.Test), Visit(node.Test)));
            }

            if (node.Iterators.Count > 0)
            {
                args.Add(Visit(nameof(node.Iterators), node.Iterators));
            }

            args.Add(new XElement(nameof(node.Body), Visit(node.Body)));

            if (node.BreakLabel != null)
            {
                args.Add(new XElement(nameof(node.BreakLabel), _parent.GetDebugView(node.BreakLabel)));
            }

            if (node.ContinueLabel != null)
            {
                args.Add(new XElement(nameof(node.ContinueLabel), _parent.GetDebugView(node.ContinueLabel)));
            }

            return Push(node, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitForEach(ForEachCSharpStatement node)
        {
            var args = new List<object>
            {
                new XElement(nameof(node.Variable), Visit(node.Variable))
            };

            if (node.Conversion != null)
            {
                args.Add(new XElement(nameof(node.Conversion), Visit(node.Conversion)));
            }

            args.Add(new XElement(nameof(node.Collection), Visit(node.Collection)));

            args.Add(new XElement(nameof(node.Body), Visit(node.Body)));

            if (node.BreakLabel != null)
            {
                args.Add(new XElement(nameof(node.BreakLabel), _parent.GetDebugView(node.BreakLabel)));
            }

            if (node.ContinueLabel != null)
            {
                args.Add(new XElement(nameof(node.ContinueLabel), _parent.GetDebugView(node.ContinueLabel)));
            }

            return Push(node, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitFromEndIndex(FromEndIndexCSharpExpression node)
        {
            var args = new List<object>
            {
                new XElement(nameof(node.Operand), Visit(node.Operand))
            };

            if (node.Method != null)
            {
                args.Add(new XAttribute(nameof(node.Method), node.Method));
            }

            return Push(node, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitGotoCase(GotoCaseCSharpStatement node)
        {
            return Push("CSharpGotoCase", node, new XAttribute(nameof(node.Value), node.Value ?? "null"));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitGotoDefault(GotoDefaultCSharpStatement node)
        {
            return Push("CSharpGotoDefault", node);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitGotoLabel(GotoLabelCSharpStatement node)
        {
            return Push(node, new XElement(nameof(node.Target), _parent.GetDebugView(node.Target)));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitIndex(IndexCSharpExpression node)
        {
            var obj = Visit(node.Object);
            var args = Visit(nameof(node.Arguments), node.Arguments, Visit);

            return Push(node, new XAttribute(nameof(node.Indexer), node.Indexer), new XElement(nameof(node.Object), obj), args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitIndexerAccess(IndexerAccessCSharpExpression node)
        {
            var obj = Visit(node.Object);
            var arg = Visit(node.Argument);

            return Push(node, new XAttribute(nameof(node.LengthOrCount), node.LengthOrCount), new XAttribute(nameof(node.IndexOrSlice), node.IndexOrSlice), new XElement(nameof(node.Object), obj), new XElement(nameof(node.Argument), arg));
        }

        protected internal override Interpolation VisitInterpolationStringLiteral(InterpolationStringLiteral node)
        {
            _nodes.Push(new XElement(nameof(InterpolationStringLiteral), new XElement("Value", node.Value)));
            return node;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitInvocation(InvocationCSharpExpression node)
        {
            var expr = Visit(node.Expression);
            var args = Visit(nameof(node.Arguments), node.Arguments, Visit);

            return Push(node, new XElement(nameof(node.Expression), expr), args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitLock(LockCSharpStatement node)
        {
            var expr = Visit(node.Expression);
            var body = Visit(node.Body);

            return Push(node, new XElement(nameof(node.Expression), expr), new XElement(nameof(node.Body), body));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitMethodCall(MethodCallCSharpExpression node)
        {
            var args = Visit(nameof(node.Arguments), node.Arguments, Visit);

            if (node.Object != null)
            {
                var obj = Visit(node.Object);
                return Push(node, new XAttribute(nameof(node.Method), node.Method), new XElement(nameof(node.Object), obj), args);
            }
            else
            {
                return Push(node, new XAttribute(nameof(node.Method), node.Method), args);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected override ParameterAssignment VisitParameterAssignment(ParameterAssignment node)
        {
            var expr = Visit(node.Expression);

            var res = new XElement(nameof(ParameterAssignment), new XAttribute(nameof(node.Parameter), node.Parameter), new XElement(nameof(node.Expression), expr));
            _nodes.Push(res);

            return node;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitWhile(WhileCSharpStatement node)
        {
            var args = new List<object>
            {
                new XElement(nameof(node.Test), Visit(node.Test)),
                new XElement(nameof(node.Body), Visit(node.Body))
            };

            if (node.BreakLabel != null)
            {
                args.Add(new XElement(nameof(node.BreakLabel), _parent.GetDebugView(node.BreakLabel)));
            }

            if (node.ContinueLabel != null)
            {
                args.Add(new XElement(nameof(node.ContinueLabel), _parent.GetDebugView(node.ContinueLabel)));
            }

            return Push(node, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitUsing(UsingCSharpStatement node)
        {
            var args = new List<object>();

            if (node.Variable != null)
            {
                args.Add(new XElement(nameof(node.Variable), Visit(node.Variable)));
            }

            args.Add(new XElement(nameof(node.Resource), Visit(node.Resource)));

            args.Add(new XElement(nameof(node.Body), Visit(node.Body)));

            return Push(node, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitConditionalAccess(ConditionalAccessCSharpExpression node)
        {
            var receiver = new XElement(nameof(node.Receiver), Visit(node.Receiver));
            var nonNullReceiver = new XElement(nameof(node.NonNullReceiver), Visit(node.NonNullReceiver));
            var whenNotNull = new XElement(nameof(node.WhenNotNull), Visit(node.WhenNotNull));

            return Push(node, receiver, nonNullReceiver, whenNotNull);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitConditionalReceiver(ConditionalReceiver node)
        {
            var id = _parent.MakeInstanceId(node);

            var res = new XElement(nameof(ConditionalReceiver), new XAttribute("Id", id), new XAttribute(nameof(node.Type), node.Type));
            _nodes.Push(res);

            return node;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitBinaryAssign(AssignBinaryCSharpExpression node)
        {
            var args = new List<object>();

            if (node.IsLifted)
            {
                args.Add(new XAttribute(nameof(node.IsLifted), node.IsLifted));
            }

            if (node.IsLiftedToNull)
            {
                args.Add(new XAttribute(nameof(node.IsLiftedToNull), node.IsLiftedToNull));
            }

            if (node.Method != null)
            {
                args.Add(new XAttribute(nameof(node.Method), node.Method));
            }

            args.Add(new XElement(nameof(node.Left), Visit(node.Left)));
            args.Add(new XElement(nameof(node.Right), Visit(node.Right)));

            if (node.LeftConversion != null)
            {
                args.Add(new XElement(nameof(node.LeftConversion), Visit(node.LeftConversion)));
            }

            if (node.FinalConversion != null)
            {
                args.Add(new XElement(nameof(node.FinalConversion), Visit(node.FinalConversion)));
            }

            return Push(node, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class never passes null reference.")]
        protected internal override Expression VisitUnaryAssign(AssignUnaryCSharpExpression node)
        {
            var args = new List<object>();

            if (node.Method != null)
            {
                args.Add(new XAttribute(nameof(node.Method), node.Method));
            }

            args.Add(new XElement(nameof(node.Operand), Visit(node.Operand)));

            return Push(node, args);
        }

        private XNode Visit(ParameterAssignment node)
        {
            VisitParameterAssignment(node);
            return _nodes.Pop();
        }
        

        private XNode Visit(ConditionalReceiver node)
        {
            VisitConditionalReceiver(node);
            return _nodes.Pop();
        }

        private XNode Visit(AwaitInfo node)
        {
            VisitAwaitInfo(node);
            return _nodes.Pop();
        }

        private XNode Visit(Interpolation node)
        {
            VisitInterpolation(node);
            return _nodes.Pop();
        }

        protected new XNode Visit(Expression expression)
        {
            return _parent.GetDebugView(expression);
        }

        protected XNode Visit(string name, IEnumerable<Expression> expressions)
        {
            var res = new List<XNode>();

            foreach (var expression in expressions)
            {
                res.Add(Visit(expression));
            }

            return new XElement(name, res);
        }

        protected static XNode Visit<T>(string name, IEnumerable<T> expressions, Func<T, XNode> visit)
        {
            var res = new List<XNode>();

            foreach (var expression in expressions)
            {
                res.Add(visit(expression));
            }

            return new XElement(name, res);
        }

        protected T Push<T>(T node, params object[] content)
            where T : CSharpExpression
        {
            return Push("CSharp" + node.CSharpNodeType.ToString(), node, content);
        }

        protected T Push<T>(string name, T node, params object[] content)
            where T : CSharpExpression
        {
            _nodes.Push(new XElement(name, new XAttribute(nameof(node.Type), node.Type), content));
            return node;
        }
    }
}
