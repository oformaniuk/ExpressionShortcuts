// Prototyping extended expression trees for C#.
//
// bartde - December 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.CSharp.Expressions
{
    // NB: ToCSharp *tries* to provide a C# fragment that's semantically equivalent to the expression
    //     tree but should be used for debugging purposes *only*. While it tries to do a good job, it's
    //     fundamentally impossible to represent all expression trees as valid C# because they have a
    //     richer set of operations, some of which cannot be represented in C# without a lowering step
    //     which would take away the original tree shape. For debugging purposes, this is good enough
    //     though, much like Reflector and ILSpy provide a reasonable means to reverse engineer what's
    //     going on. Although these fundamental restrictions exist, we try to do our best to emit good
    //     and concise C# code, but there's a long tail of room for improvement should this become an
    //     often-used mechanism.

    partial class CSharpExpression : IDebugViewExpression, ICSharpPrintableExpression
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        void ICSharpPrintableExpression.Accept(ICSharpPrintingVisitor visitor)
        {
            Accept(visitor);
        }

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        protected abstract void Accept(ICSharpPrintingVisitor visitor);

        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        int ICSharpPrintableExpression.Precedence => Precedence;

        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected abstract int Precedence { get; }

        /// <summary>
        /// Gets a value indicating whether the node represents a statement.
        /// </summary>
        bool ICSharpPrintableExpression.IsStatement => IsStatement;

        /// <summary>
        /// Gets a value indicating whether the node represents a statement.
        /// </summary>
        protected virtual bool IsStatement => false;

        /// <summary>
        /// Gets a value indicating whether the node represents an operation that supports overflow checking.
        /// </summary>
        bool ICSharpPrintableExpression.HasCheckedMode => HasCheckedMode;

        /// <summary>
        /// Gets a value indicating whether the node represents an operation that supports overflow checking.
        /// </summary>
        protected virtual bool HasCheckedMode => false;

        /// <summary>
        /// Gets a value indicating whether the node performs overflow checking.
        /// </summary>
        bool ICSharpPrintableExpression.IsChecked => IsChecked;

        /// <summary>
        /// Gets a value indicating whether the node performs overflow checking.
        /// </summary>
        protected virtual bool IsChecked => false;

        /// <summary>
        /// Gets a value indicating whether the node represents a lambda expression.
        /// </summary>
        bool ICSharpPrintableExpression.IsLambda => IsLambda;

        /// <summary>
        /// Gets a value indicating whether the node represents a lambda expression.
        /// </summary>
        protected virtual bool IsLambda => false;

        /// <summary>
        /// Gets a value indicating whether the node represents a block expression.
        /// </summary>
        bool ICSharpPrintableExpression.IsBlock => IsBlock;

        /// <summary>
        /// Gets a value indicating whether the node represents a block expression.
        /// </summary>
        protected virtual bool IsBlock => false;
    }

    partial class MethodCallCSharpExpression
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Call);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            if (Object != null)
            {
                visitor.ParenthesizedVisit(this, Object);
            }
            else
            {
                visitor.Out(visitor.ToCSharp(Method.DeclaringType));
            }

            visitor.Out(".");
            visitor.Out(Method.Name);

            if (Method.IsGenericMethod && !CSharpLanguageHelpers.CanInferGenericArguments(Method))
            {
                var genArgs = string.Join(", ", Method.GetGenericArguments().Select(visitor.ToCSharp));
                visitor.Out("<");
                visitor.Out(genArgs);
                visitor.Out(">");
            }

            visitor.Out("(");
            visitor.ArgsVisit(Arguments);
            visitor.Out(")");
        }
    }

    partial class IndexCSharpExpression
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Index);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.ParenthesizedVisit(this, Object);

            visitor.Out("[");
            visitor.ArgsVisit(Arguments); // TODO: Indexer could have non-default name
            visitor.Out("]");
        }
    }

    partial class InvocationCSharpExpression
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Invoke);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.ParenthesizedVisit(this, Expression);

            visitor.Out("(");
            visitor.ArgsVisit(Arguments);
            visitor.Out(")");
        }
    }

    partial class CSharpStatement
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Block /* a statement */);

        /// <summary>
        /// Gets a value indicating whether the node represents a statement.
        /// </summary>
        protected override bool IsStatement => true;
    }

    partial class WhileCSharpStatement
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Out("while (");
            visitor.VisitExpression(Test);
            visitor.Out(")");

            visitor.VisitLoopBody(this);
        }
    }

    partial class DoCSharpStatement
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            // TODO: break/continue label analysis?

            visitor.Out("do");

            visitor.VisitLoopBody(this, needsCurlies: true);

            visitor.NewLine();
            visitor.Out("while (");
            visitor.VisitExpression(Test);
            visitor.Out(");");
        }
    }

    partial class ForCSharpStatement
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            var hasUniformVariables = true;
            var hasTopLevelBlock = false;

            var variableType = default(Type);
            var variables = new HashSet<ParameterExpression>(Variables);

            if (variables.Count > 0)
            {
                foreach (var init in Initializers)
                {
                    var lhs = default(Expression);

                    if (init.NodeType == ExpressionType.Assign)
                    {
                        var assign = (BinaryExpression)init;
                        lhs = assign.Left;
                    }
                    else
                    {
                        if (init is AssignBinaryCSharpExpression assign)
                        {
                            lhs = assign.Left;
                        }
                    }

                    if (!(lhs is ParameterExpression variable) || !variables.Remove(variable))
                    {
                        hasUniformVariables = false;
                        break;
                    }

                    if (variableType == null)
                    {
                        variableType = variable.Type;
                    }
                    
                    else if (!variableType.IsEquivalentTo(variable.Type))
                    {
                        hasUniformVariables = false;
                        break;
                    }
                }

                if (!hasUniformVariables || variableType == null)
                {
                    hasTopLevelBlock = true;

                    visitor.Out("{");
                    visitor.Indent();
                    visitor.NewLine();

                    var vars = Variables.ToLookup(v => v.Type, v => v);
                    foreach (var kv in vars)
                    {
                        visitor.Out(visitor.ToCSharp(kv.Key));
                        visitor.Out(" ");
                        visitor.Out(string.Join(", ", kv.Select(v => visitor.GetVariableName(v, true))));
                        visitor.Out(";");
                        visitor.NewLine();
                    }

                    visitor.Out("for (");
                }
                else
                {
                    visitor.Out("for (");
                    visitor.Out(visitor.ToCSharp(variableType));
                    visitor.Out(" ");
                }
            }
            else
            {
                visitor.Out("for (");
            }

            var n = Initializers.Count;
            for (var i = 0; i < n; i++)
            {
                visitor.VisitExpression(Initializers[i]);

                if (i != n - 1)
                {
                    visitor.Out(", ");
                }
            }

            visitor.Out(";");

            if (Test != null)
            {
                visitor.Out(" ");
                visitor.VisitExpression(Test);
            }

            visitor.Out(";");

            var m = Iterators.Count;

            if (m > 0)
            {
                visitor.Out(" ");

                for (var i = 0; i < m; i++)
                {
                    visitor.VisitExpression(Iterators[i]);

                    if (i != m - 1)
                    {
                        visitor.Out(", ");
                    }
                }
            }

            visitor.Out(")");

            visitor.VisitLoopBody(this);

            if (hasTopLevelBlock)
            {
                visitor.Dedent();
                visitor.NewLine();
                visitor.Out("}");
            }
        }
    }

    partial class ForEachCSharpStatement
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            // TODO: break/continue label analysis?

            visitor.Out("foreach (");

            visitor.Out(visitor.ToCSharp(Variable.Type));
            visitor.Out(" ");
            visitor.Out(visitor.GetVariableName(Variable, true));

            visitor.Out(" in ");

            visitor.VisitExpression(Collection);

            visitor.Out(")");

            visitor.VisitLoopBody(this);
        }
    }
    
    partial class UsingCSharpStatement
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Out("using (");

            if (Variable != null)
            {
                visitor.Out(visitor.ToCSharp(Variable.Type));
                visitor.Out(" ");
                visitor.Out(visitor.GetVariableName(Variable, true));
                visitor.Out(" = ");
            }

            visitor.VisitExpression(Resource);

            visitor.Out(")");

            visitor.VisitBlockLike(Body);
        }
    }

    partial class LockCSharpStatement
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Out("lock (");
            visitor.VisitExpression(Expression);
            visitor.Out(")");

            visitor.VisitBlockLike(Body);
        }
    }

    partial class GotoCSharpStatement
    {
        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            switch (Kind)
            {
                case CSharpGotoKind.GotoCase:
                    var gotoCase = (GotoCaseCSharpStatement)this;
                    var value = gotoCase.Value;
                    visitor.Out("goto case ");
                    visitor.Literal(value, value?.GetType() ?? typeof(object));
                    visitor.Out(";");
                    break;
                case CSharpGotoKind.GotoDefault:
                    visitor.Out("goto default;");
                    break;
                case CSharpGotoKind.GotoLabel:
                    var gotoLabel = (GotoLabelCSharpStatement)this;
                    visitor.Out($"goto {visitor.GetLabelName(gotoLabel.Target)};");
                    break;
            }
        }
    }

    partial class BlockCSharpExpression
    {
        /// <summary>
        /// Gets a value indicating whether the node represents a block expression.
        /// </summary>
        protected override bool IsBlock => true;

        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Block);

        /// <summary>
        /// Gets a value indicating whether the node represents a statement.
        /// </summary>
        protected override bool IsStatement => true;

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Out("{");
            visitor.Indent();
            visitor.NewLine();

            var vars = Variables.ToLookup(v => v.Type, v => v);
            foreach (var kv in vars)
            {
                visitor.Out(visitor.ToCSharp(kv.Key));
                visitor.Out(" ");
                visitor.Out(string.Join(", ", kv.Select(v => visitor.GetVariableName(v, true))));
                visitor.Out(";");
                visitor.NewLine();
            }

            var n = Statements.Count;
            for (var i = 0; i < n; i++)
            {
                var expr = Statements[i];

                if (i == n - 1 && Type != typeof(void))
                {
                    var hasExplicitReturn = false;

                    if (expr.NodeType == ExpressionType.Goto)
                    {
                        var gotoExpr = (GotoExpression)expr;
                        if (gotoExpr.Kind == GotoExpressionKind.Return)
                        {
                            hasExplicitReturn = true;
                        }
                    }

                    if (!hasExplicitReturn)
                    {
                        visitor.Out("return ");
                    }

                    visitor.VisitExpression(expr);

                    if (!hasExplicitReturn)
                    {
                        visitor.Out(";");
                    }
                }
                else
                {
                    visitor.Visit(expr);
                }

                if (i != n - 1)
                {
                    visitor.NewLine();
                }
            }

            visitor.Dedent();
            visitor.NewLine();
            visitor.Out("}");
        }
    }

    partial class AsyncLambdaCSharpExpression
    {
        /// <summary>
        /// Gets a value indicating whether the node represents a lambda expression.
        /// </summary>
        protected override bool IsLambda => true;

        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Lambda);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            // TODO: No guarantee delegate type can be inferred; add `new` every time or track info?

            visitor.Out("async (");

            var n = Parameters.Count;
            for (var i = 0; i < n; i++)
            {
                var parameter = Parameters[i];

                visitor.Out(visitor.ToCSharp(parameter.Type));
                visitor.Out(" ");

                var name = visitor.GetVariableName(parameter, true);
                visitor.Out(name);

                if (i != n - 1)
                {
                    visitor.Out(", ");
                }
            }

            visitor.Out(") =>");

            var isStatementLambda = visitor.IsBlock(Body) || visitor.IsStatement(Body);

            if (isStatementLambda)
            {
                visitor.VisitBlockLike(Body, needsCurlies: true);
            }
            else
            {
                visitor.Out(" ");
                visitor.Visit(Body);
            }
        }
    }

    partial class AwaitCSharpExpression
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Negate /* another unary operation */);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Out("await ");
            visitor.ParenthesizedVisit(this, Operand);
        }
    }

    partial class ConditionalAccessCSharpExpression<TExpression>
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.MemberAccess /* one of the conditionally accessed nodes */);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            // TODO: ensure parentheses are added if needed

            visitor.ParenthesizedVisit(this, Receiver);
            visitor.Out("?");
            visitor.Visit(WhenNotNull);
        }
    }

    partial class ConditionalReceiver
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Parameter /* never parenthesized */);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
        }
    }

    partial class AssignUnaryCSharpExpression
    {
        /// <summary>
        /// Gets a value indicating whether the node represents an operation that supports overflow checking.
        /// </summary>
        protected override bool HasCheckedMode
        {
            get
            {
                var res = false;

                switch (CSharpNodeType)
                {
                    case CSharpExpressionType.PostIncrementAssign:
                    case CSharpExpressionType.PostIncrementAssignChecked:
                    case CSharpExpressionType.PostDecrementAssign:
                    case CSharpExpressionType.PostDecrementAssignChecked:
                    case CSharpExpressionType.PreIncrementAssign:
                    case CSharpExpressionType.PreIncrementAssignChecked:
                    case CSharpExpressionType.PreDecrementAssign:
                    case CSharpExpressionType.PreDecrementAssignChecked:
                        res = true;
                        break;
                }

                return res;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the node performs overflow checking.
        /// </summary>
        protected override bool IsChecked
        {
            get
            {
                switch (CSharpNodeType)
                {
                    case CSharpExpressionType.PostIncrementAssignChecked:
                    case CSharpExpressionType.PostDecrementAssignChecked:
                    case CSharpExpressionType.PreIncrementAssignChecked:
                    case CSharpExpressionType.PreDecrementAssignChecked:
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence
        {
            get
            {
                switch (CSharpNodeType)
                {
                    case CSharpExpressionType.PostDecrementAssign:
                    case CSharpExpressionType.PostDecrementAssignChecked:
                    case CSharpExpressionType.PostIncrementAssign:
                    case CSharpExpressionType.PostIncrementAssignChecked:
                        return CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.PostDecrementAssign);
                }

                return CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.PreDecrementAssign);
            }
        }

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            var nodeType = ConvertNodeType(CSharpNodeType);
            var op = CSharpLanguageHelpers.GetOperatorSyntax(nodeType);
            var mtd = CSharpLanguageHelpers.GetClsMethodName(nodeType);
            var isChecked = false;
            var isSuffix = false;
            var asMethod = false;

            switch (CSharpNodeType)
            {
                case CSharpExpressionType.PreDecrementAssignChecked:
                case CSharpExpressionType.PreIncrementAssignChecked:
                    isChecked = true;
                    break;
                case CSharpExpressionType.PostDecrementAssign:
                case CSharpExpressionType.PostIncrementAssign:
                    isSuffix = true;
                    break;
                case CSharpExpressionType.PostDecrementAssignChecked:
                case CSharpExpressionType.PostIncrementAssignChecked:
                    isSuffix = true;
                    isChecked = true;
                    break;
            }

            if (mtd != null && Method != null)
            {
                if (Method.Name != mtd) // TODO: check declaring type
                {
                    asMethod = true;
                }
            }

            if (asMethod)
            {
                // NB: Not preserving evaluation semantics for indexers etc.
                if (op.Contains("++") || op.Contains("--"))
                {
                    // TODO: doesn't deal with post/pre logic yet
                    visitor.Visit(Operand);
                    visitor.Out(" = ");
                }

                visitor.StaticMethodCall(Method, Operand);
                return;
            }

            var hasEnteredChecked = false;

            if (!visitor.InCheckedContext && isChecked)
            {
                var scan = new CSharpCheckedOpScanner();

                scan.Visit(Operand);

                if (!scan.HasUncheckedOperation)
                {
                    visitor.InCheckedContext = true;
                    hasEnteredChecked = true;

                    visitor.Out("checked(");
                }
                else
                {
                    // NB: Produces invalid C#; we'd have to spill expressions into locals if we want
                    //     to emit valid C# in this case, at the expense of losing the tree shape.
                    op = $"/*checked(*/{op}/*)*/";
                    isChecked = false;
                }

                // NB: Could produce invalid C# if assign node is used as a statement; need to keep
                //     track of the parent operation to determine this.
            }

            if (!isSuffix)
            {
                visitor.Out(op);
            }

            visitor.ParenthesizedVisit(this, Operand);

            if (isSuffix)
            {
                visitor.Out(op);
            }

            if (hasEnteredChecked)
            {
                visitor.InCheckedContext = false;
                visitor.Out(")");
            }
        }
    }

    partial class AssignBinaryCSharpExpression
    {
        /// <summary>
        /// Gets a value indicating whether the node represents an operation that supports overflow checking.
        /// </summary>
        protected override bool HasCheckedMode
        {
            get
            {
                var res = false;

                switch (CSharpNodeType)
                {
                    case CSharpExpressionType.AddAssign:
                    case CSharpExpressionType.AddAssignChecked:
                    case CSharpExpressionType.SubtractAssign:
                    case CSharpExpressionType.SubtractAssignChecked:
                    case CSharpExpressionType.MultiplyAssign:
                    case CSharpExpressionType.MultiplyAssignChecked:
                        res = true;
                        break;
                }

                return res;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the node performs overflow checking.
        /// </summary>
        protected override bool IsChecked
        {
            get
            {
                switch (CSharpNodeType)
                {
                    case CSharpExpressionType.AddAssignChecked:
                    case CSharpExpressionType.SubtractAssignChecked:
                    case CSharpExpressionType.MultiplyAssignChecked:
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Assign);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            var nodeType = ConvertNodeType(CSharpNodeType);
            var op = CSharpNodeType == CSharpExpressionType.NullCoalescingAssign ? "??=" : CSharpLanguageHelpers.GetOperatorSyntax(nodeType);
            var mtd = CSharpLanguageHelpers.GetClsMethodName(nodeType);
            var isChecked = false;
            var asMethod = false;

            switch (CSharpNodeType)
            {
                case CSharpExpressionType.AddAssignChecked:
                case CSharpExpressionType.MultiplyAssignChecked:
                case CSharpExpressionType.SubtractAssignChecked:
                    isChecked = true;
                    break;
            }

            if (mtd != null && Method != null)
            {
                if (Method.Name != mtd) // TODO: check declaring type
                {
                    asMethod = true;
                }
            }

            if (asMethod)
            {
                // NB: Not preserving evaluation semantics for indexers etc.
                visitor.Visit(Left);
                visitor.Out(" = ");
                visitor.StaticMethodCall(Method, Left, Right);
                return;
            }

            var hasEnteredChecked = false;

            if (!visitor.InCheckedContext && isChecked)
            {
                var scan = new CSharpCheckedOpScanner();

                scan.Visit(Left);

                if (!scan.HasUncheckedOperation)
                {
                    scan.Visit(Right);
                }

                if (!scan.HasUncheckedOperation)
                {
                    visitor.InCheckedContext = true;
                    hasEnteredChecked = true;

                    visitor.Out("checked(");
                }
                else
                {
                    // NB: Produces invalid C#; we'd have to spill expressions into locals if we want
                    //     to emit valid C# in this case, at the expense of losing the tree shape.
                    op = $"/*checked(*/{op}/*)*/";
                    isChecked = false;
                }

                // NB: Could produce invalid C# if assign node is used as a statement; need to keep
                //     track of the parent operation to determine this.
            }

            visitor.ParenthesizedVisit(this, Left);

            visitor.Out(" ");
            visitor.Out(op);
            visitor.Out(" ");

            visitor.ParenthesizedVisit(this, Right);

            if (hasEnteredChecked)
            {
                visitor.InCheckedContext = false;
                visitor.Out(")");
            }
        }
    }
    
    partial class DiscardCSharpExpression
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.Parameter);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Out("_");
        }
    }
    
    partial class FromEndIndexCSharpExpression
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.UnaryPlus);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Out("^");
            visitor.Visit(Operand);
        }
    }

    partial class ArrayAccessCSharpExpression
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.ArrayIndex);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Visit(Array);
            visitor.Out("[");
            visitor.Visit(Indexes);
            visitor.Out("]");
        }
    }

    partial class IndexerAccessCSharpExpression
    {
        /// <summary>
        /// Gets the precedence level of the expression.
        /// </summary>
        protected override int Precedence => CSharpLanguageHelpers.GetOperatorPrecedence(ExpressionType.ArrayIndex);

        /// <summary>
        /// Dispatches the current node to the specified visitor.
        /// </summary>
        /// <param name="visitor">Visitor to dispatch to.</param>
        /// <returns>The result of visiting the node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Base class doesn't pass null.")]
        protected override void Accept(ICSharpPrintingVisitor visitor)
        {
            visitor.Visit(Object);
            visitor.Out("[");
            visitor.Visit(Argument);
            visitor.Out("]");
        }
    }

    static class CSharpPrintingVisitorExtensions
    {
        public static void Visit(this ICSharpPrintingVisitor visitor, IList<Expression> args)
        {
            var n = args.Count;

            for (var i = 0; i < n; i++)
            {
                visitor.Visit(args[i]);

                if (i != n - 1)
                {
                    visitor.Out(", ");
                }
            }
        }

        public static void ArgsVisit(this ICSharpPrintingVisitor visitor, IList<ParameterAssignment> args)
        {
            var requiresNamedArgs = false;
            var n = args.Count;

            for (var i = 0; i < n; i++)
            {
                var arg = args[i];
                var par = arg.Parameter;

                if (requiresNamedArgs || par.Position != i)
                {
                    requiresNamedArgs = true;

                    visitor.Out(par.Name + ": ");
                }

                if (par.ParameterType.IsByRef)
                {
                    if (par.IsOut)
                    {
                        visitor.Out("out ");
                    }
                    else
                    {
                        visitor.Out("ref ");
                    }
                }

                visitor.Visit(arg.Expression);

                if (i != n - 1)
                {
                    visitor.Out(", ");
                }
            }
        }

        public static void VisitLoopBody(this ICSharpPrintingVisitor visitor, LoopCSharpStatement loop, bool needsCurlies = false)
        {
            visitor.PushBreak(loop.BreakLabel);
            visitor.PushContinue(loop.ContinueLabel);

            visitor.VisitBlockLike(loop.Body, needsCurlies);

            visitor.PopContinue();
            visitor.PopBreak();
        }
    }

    class CSharpCheckedOpScanner : CheckedOpScanner
    {
        private readonly CSharpVisitor _csharp;

        public CSharpCheckedOpScanner()
        {
            _csharp = new CSharpVisitor(base.Visit);
        }

        protected override Expression VisitExtension(Expression node)
        {
            // NB: Prevents reduction of nodes by the visitor used in the debugging library.

            if (node is CSharpExpression csharp)
            {
                _csharp.Analyze(csharp);
                return node;
            }

            return node;
        }

        class CSharpVisitor : CSharpExpressionVisitor
        {
            private readonly Func<Expression, Expression> _visit;

            public CSharpVisitor(Func<Expression, Expression> visit)
            {
                _visit = visit;
            }

            public void Analyze(CSharpExpression node)
            {
                // NB: Don't call our own Visit at the top-level; it'd cause another call `_visit`.
                base.Visit(node);
            }

            public override Expression Visit(Expression node)
            {
                _visit(node);

                return base.Visit(node);
            }
        }
    }
}