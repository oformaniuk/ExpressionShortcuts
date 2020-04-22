using System.Linq.Expressions;

namespace Expressions.Shortcuts
{
    /// <summary>
    /// Wrapper around of <see cref="System.Linq.Expressions.Expression"/> to provide addition functionality
    /// </summary>
    internal class ExpressionContainer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        public ExpressionContainer(Expression expression) => Expression = expression;

        /// <summary>
        /// Return the underling <see cref="System.Linq.Expressions.Expression"/>
        /// </summary>
        public virtual Expression Expression { get; }

        /// <summary>
        /// Creates typed representation <see cref="ExpressionContainer{T}"/> of current <see cref="ExpressionContainer"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ExpressionContainer<T> Typed<T>() => new ExpressionContainer<T>(Expression);
        
        /// <summary>
        /// Creates <see cref="TypeBinaryExpression"/> from current <see cref="Expression"/>
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <returns></returns>
        public ExpressionContainer<bool> Is<TV>() => new ExpressionContainer<bool>(Expression.TypeIs(Expression, typeof(TV)));
        
        /// <summary>
        /// Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an explicit reference or boxing conversion where <see langword="null" /> is supplied if the conversion fails.
        /// </summary>
        /// <typeparam name="TV">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</typeparam>
        /// <returns></returns>
        public ExpressionContainer<TV> As<TV>() => new ExpressionContainer<TV>(Expression.TypeAs(Expression, typeof(TV)));
        
        /// <summary>
        /// Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a type conversion operation.
        /// </summary>
        /// <typeparam name="TV">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</typeparam>
        /// <returns></returns>
        public ExpressionContainer<TV> Cast<TV>() => new ExpressionContainer<TV>(Expression.Convert(Expression, typeof(TV)));
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="expressionContainer"></param>
        /// <returns></returns>
        public static implicit operator Expression(ExpressionContainer expressionContainer) => expressionContainer.Expression;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static implicit operator ExpressionContainer(Expression expression) => new ExpressionContainer(expression);
    }
    
    /// <summary>
    /// Provides strongly typed container for <see cref="Expression"/>.
    /// </summary>
    /// <remarks>Used to trick C# compiler in cases like <see cref="ExpressionShortcuts.Call"/> in order to pass value to target method.</remarks>
    /// <typeparam name="T">Type of expected <see cref="Expression"/> result value.</typeparam>
    internal class ExpressionContainer<T> : ExpressionContainer
    {
        /// <summary>
        /// Used to trick C# compiler
        /// </summary>
        public static implicit operator T(ExpressionContainer<T> expressionContainer) => default(T);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        public ExpressionContainer(Expression expression) : base(expression)
        {
        }
    }
}