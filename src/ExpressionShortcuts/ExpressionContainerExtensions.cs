using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Expressions.Shortcuts
{
    internal static partial class ExpressionShortcuts
    {
        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.Property(System.Linq.Expressions.Expression,System.String)"/>
        /// </summary>
        /// <param name="instance"/>
        /// <param name="propertyAccessor">Property accessor expression</param>
        /// <typeparam name="T">Expected type of resulting target <see cref="Expression"/></typeparam>
        /// <typeparam name="TV">Expected type of resulting <see cref="MemberExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Property<T, TV>(this ExpressionContainer<T> instance, Expression<Func<T, TV>> propertyAccessor)
        {
            return Property(instance.Expression, propertyAccessor);
        }

        /// <summary>
        /// Creates <see cref="MethodCallExpression"/> or <see cref="InvocationExpression"/> based on <paramref name="invocationExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        /// <param name="instance"/>
        /// <param name="invocationExpression">Expression used to invoke the method.</param>
        /// <returns><see cref="Void"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Call<T>(this ExpressionContainer<T> instance, Expression<Action<T>> invocationExpression)
        {
            return new ExpressionContainer(ExpressionUtils.ProcessCallLambda(invocationExpression, instance));
        }
        
        /// <summary>
        /// Creates <see cref="MethodCallExpression"/> or <see cref="InvocationExpression"/> based on <paramref name="invocationExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        /// <param name="instance"/>
        /// <param name="invocationExpression">Expression used to invoke the method.</param>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Call<T, TV>(this ExpressionContainer<T> instance, Expression<Func<T, TV>> invocationExpression)
        {
            return Arg<TV>(ExpressionUtils.ProcessCallLambda(invocationExpression, instance));
        }

        /// <summary>
        /// Executes code directly by passing <paramref name="code"/> to <see cref="Call"/>
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="code"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TV"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Code<T, TV>(this ExpressionContainer<T> instance, Func<T, TV> code)
        {
            return Call(() => code(instance));
        }
        
        /// <summary>
        /// Executes code directly by passing <paramref name="code"/> to <see cref="Call"/>
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="code"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Code<T>(this ExpressionContainer<T> instance, Action<T> code)
        {
            return Call(() => code(instance));
        }
        
        /// <summary>
        /// Creates <see cref="TryExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        /// <param name="instance"/>
        /// <param name="blockBody">Expressions used inside of <c>try</c> block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Using<T>(this ExpressionContainer<T> instance, Action<ExpressionContainer<T>, BlockBuilder> blockBody) where T : IDisposable
        {
            var variable = Var<T>();
            
            return Try()
                .Body(block => blockBody(variable, block.Parameter(variable, instance)))
                .Finally(instance.Call(o => o.Dispose()));
        }

        /// <summary>
        /// Created <see langword="return"/> statement
        /// </summary>
        /// <param name="instance"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<Expression> Return<T>(this ExpressionContainer<T> instance)
        {
            var returnTarget = Expression.Label(typeof(T));
            var returnExpression = Expression.Return(returnTarget, instance, typeof(T));
            var returnLabel = Expression.Label(returnTarget, Null<T>());

            return new Expression[]{returnExpression, returnLabel};
        }

        /// <summary>
        /// Creates assign <see cref="BinaryExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Assign<T>(this ExpressionContainer<T> target, ExpressionContainer<T> value)
        {
            return new ExpressionContainer(Expression.Assign(target, value));
        }
        
        /// <summary>
        /// Creates assign <see cref="BinaryExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Assign<T>(this ExpressionContainer<T> target, T value)
        {
            return new ExpressionContainer(Expression.Assign(target, Expression.Constant(value, typeof(T))));
        }
        
        /// <summary>
        /// Creates assign <see cref="BinaryExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Assign<T>(this ExpressionContainer<T> target, Expression value)
        {
            return new ExpressionContainer(Expression.Assign(target, value));
        }
        
        /// <summary>
        /// Creates ternary assignment like <code>target = condition ? ifTrue : ifFalse</code>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Expression TernaryAssign<T>(this ExpressionContainer<T> target, ExpressionContainer<bool> condition, ExpressionContainer<T> ifTrue, ExpressionContainer<T> ifFalse)
        {
            return Expression.IfThenElse(
                condition.Expression,
                Expression.Assign(target, ifTrue),
                Expression.Assign(target, ifFalse));
        }
    }
}