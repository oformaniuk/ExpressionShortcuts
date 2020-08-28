﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CSharp.Expressions;

namespace Expressions.Shortcuts
{
    /// <summary>
    /// Stands for <see cref="Expression"/> shortcuts.
    /// </summary>
    internal static partial class ExpressionShortcuts
    {
        /// <summary>
        /// Creates strongly typed representation of the <paramref name="expression"/>
        /// </summary>
        /// <remarks>If <paramref name="expression"/> is <c>null</c> returns  result of <see cref="Null{T}"/></remarks>
        /// <param name="expression"><see cref="Expression"/> to wrap</param>
        /// <typeparam name="T">Expected type of resulting <see cref="Expression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Arg<T>(Expression expression) => expression == null ? Null<T>() : new ExpressionContainer<T>(expression);
        
        /// <summary>
        /// Creates strongly typed representation of the <see cref="ExpressionContainer.Expression"/>
        /// </summary>
        /// <remarks>If <paramref name="value"/> is <c>null</c> returns  result of <see cref="Null{T}"/></remarks>
        /// <param name="value"><paramref name="value"/> to wrap</param>
        /// <typeparam name="T">Expected type of resulting <see cref="Expression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Arg<T>(T value) => value == null ? Null<T>() : new ExpressionContainer<T>(Expression.Constant(value, typeof(T)));

        /// <summary>
        /// Creates strongly typed representation of the <paramref name="expression"/>.
        /// </summary>
        /// <remarks>If <paramref name="expression"/> is <c>null</c> returns  result of <see cref="Null{T}"/></remarks>
        /// <param name="expression"><see cref="Expression"/> to wrap</param>
        /// <typeparam name="T">Expected type of resulting <see cref="Expression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Arg<T>(Expression<T> expression) => expression == null ? Null<T>() : new ExpressionContainer<T>(expression);
        
        /// <summary>
        /// Creates strongly typed representation of the <paramref name="expression"/> and performs <see cref="Expression.Convert(System.Linq.Expressions.Expression,System.Type)"/> on it.
        /// </summary>
        /// <remarks>If <paramref name="expression"/> is <c>null</c> returns  result of <see cref="Null{T}"/></remarks>
        /// <param name="expression"><see cref="Expression"/> to wrap</param>
        /// <typeparam name="T">Expected type of resulting <see cref="Expression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Cast<T>(Expression expression) => expression == null ? Null<T>() : new ExpressionContainer<T>(Expression.Convert(expression, typeof(T)));

        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.Variable(System.Type, System.String)"/>
        /// </summary>
        /// <param name="name">Variable name. Corresponds to type name if omitted.</param>
        /// <typeparam name="T">Expected type of resulting <see cref="ParameterExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Var<T>(string name = null)
        {
            return new ExpressionContainer<T>(Expression.Variable(typeof(T), name ?? typeof(T).Name));
        }
        
        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.Parameter(System.Type, System.String)"/>
        /// </summary>
        /// <param name="name">Variable name. Corresponds to type name if omitted.</param>
        /// <typeparam name="T">Expected type of resulting <see cref="ParameterExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Parameter<T>(string name = null)
        {
            return new ExpressionContainer<T>(Expression.Parameter(typeof(T), name ?? typeof(T).Name));
        }


        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.Property(System.Linq.Expressions.Expression,System.String)"/>
        /// </summary>
        /// <param name="instance">Variable name. Corresponds to type name if omitted.</param>
        /// <param name="propertyLambda">Property accessor expression</param>
        /// <typeparam name="T">Expected type of resulting target <see cref="Expression"/></typeparam>
        /// <typeparam name="TV">Expected type of resulting <see cref="MemberExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Property<T, TV>(Expression instance, Expression<Func<T, TV>> propertyLambda)
        {
            return Arg<TV>(ExpressionUtils.ProcessPropertyLambda(instance, propertyLambda));
        }

        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.Property(System.Linq.Expressions.Expression,System.String)"/>
        /// </summary>
        /// <param name="instance">Variable name. Corresponds to type name if omitted.</param>
        /// <param name="propertyName"/>
        /// <typeparam name="TV">Expected type of resulting <see cref="MemberExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Property<TV>(Expression instance, string propertyName)
        {
            return Arg<TV>(Expression.Property(instance, propertyName));
        }
        
        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.Field(System.Linq.Expressions.Expression,System.String)"/>
        /// </summary>
        /// <param name="instance">Variable name. Corresponds to type name if omitted.</param>
        /// <param name="propertyLambda">Property accessor expression</param>
        /// <typeparam name="T">Expected type of resulting target <see cref="Expression"/></typeparam>
        /// <typeparam name="TV">Expected type of resulting <see cref="MemberExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Field<T, TV>(Expression instance, Expression<Func<T, TV>> propertyLambda)
        {
            return Arg<TV>(ExpressionUtils.ProcessFieldLambda(instance, propertyLambda));
        }
        
        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.Field(System.Linq.Expressions.Expression,System.String)"/>
        /// </summary>
        /// <param name="instance">Variable name. Corresponds to type name if omitted.</param>
        /// <param name="propertyName"/>
        /// <typeparam name="TV">Expected type of resulting <see cref="MemberExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Field<TV>(Expression instance, string propertyName)
        {
            return Arg<TV>(Expression.Field(instance, propertyName));
        }
        
        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.PropertyOrField(System.Linq.Expressions.Expression,System.String)"/>
        /// </summary>
        /// <param name="instance">Variable name. Corresponds to type name if omitted.</param>
        /// <param name="propertyLambda">Property accessor expression</param>
        /// <typeparam name="T">Expected type of resulting target <see cref="Expression"/></typeparam>
        /// <typeparam name="TV">Expected type of resulting <see cref="MemberExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Member<T, TV>(Expression instance, Expression<Func<T, TV>> propertyLambda)
        {
            return Arg<TV>(ExpressionUtils.ProcessMemberLambda(instance, propertyLambda));
        }
        
        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.PropertyOrField(System.Linq.Expressions.Expression,System.String)"/>
        /// </summary>
        /// <param name="instance">Variable name. Corresponds to type name if omitted.</param>
        /// <param name="propertyName"/>
        /// <typeparam name="TV">Expected type of resulting <see cref="MemberExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<TV> Member<TV>(Expression instance, string propertyName)
        {
            return Arg<TV>(Expression.PropertyOrField(instance, propertyName));
        }

        /// <summary>
        /// Creates strongly typed representation of the <see cref="Expression.NewArrayInit(System.Type,System.Collections.Generic.IEnumerable{System.Linq.Expressions.Expression})"/>
        /// </summary>
        /// <param name="items">Items for the new array</param>
        /// <typeparam name="T">Expected type of resulting <see cref="NewArrayExpression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T[]> Array<T>(IEnumerable<Expression> items)
        {
            return Arg<T[]>(Expression.NewArrayInit(typeof(T), items));
        }

        /// <summary>
        /// Creates <see cref="MethodCallExpression"/> or <see cref="InvocationExpression"/> based on <paramref name="invocationExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        /// <param name="invocationExpression">Expression used to invoke the method.</param>
        /// <returns><see cref="Void"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Call(Expression<Action> invocationExpression)
        {
            return new ExpressionContainer(ExpressionUtils.ProcessCallLambda(invocationExpression));
        }

        /// <summary>
        /// Creates <see cref="MethodCallExpression"/> or <see cref="InvocationExpression"/> based on <paramref name="invocationExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        /// <param name="invocationExpression">Expression used to invoke the method.</param>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Call<T>(Expression<Func<T>> invocationExpression)
        {
            return Arg<T>(ExpressionUtils.ProcessCallLambda(invocationExpression));
        }
        
        // /// <summary>
        // /// Creates <see cref="MethodCallExpression"/> or <see cref="InvocationExpression"/> based on <paramref name="invocationExpression"/>.
        // /// Parameters are resolved based on actual passed parameters.
        // /// </summary>
        // /// <param name="invocationExpression">Expression used to invoke the method.</param>
        // /// <returns><see cref="ExpressionContainer{T}"/></returns>
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static ExpressionContainer<Task<T>> Call<T>(Expression<Func<Task<T>>> invocationExpression)
        // {
        //     return Arg<Task<T>>(ExpressionUtils.ProcessCallLambda(invocationExpression));
        // }

        /// <summary>
        /// Creates <see cref="NewExpression"/>. Parameters for constructor and constructor itself are resolved based <paramref name="invocationExpression"/>.
        /// </summary>
        /// <param name="invocationExpression">Expression used to invoke the method.</param>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> New<T>(Expression<Func<T>> invocationExpression)
        {
            return Arg<T>(ExpressionUtils.ProcessCallLambda(invocationExpression));
        }
        
        /// <summary>
        /// Creates <see cref="NewExpression"/> using default constructor.
        /// </summary>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> New<T>() where T: new()
        {
            return Arg<T>(Expression.New(typeof(T).GetConstructor(new Type[0])));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Await<T>(Expression<Func<Task<T>>> expression)
        {
            return Arg<T>(CSharpExpression.Await(ExpressionUtils.ProcessCallLambda(expression)));
        }
        
        /// <summary>
        /// Provides fluent interface for <see cref="BlockExpression"/> creation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockBuilder Block(Type returnType = null)
        {
            return new BlockBuilder(returnType);
        }

        /// <summary>
        /// Creates strongly typed representation of <c>null</c>.
        /// </summary>
        /// <typeparam name="T">Expected type of resulting <see cref="Expression"/></typeparam>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Null<T>()
        {
            return Arg<T>(Null(typeof(T)));
        }
        
        /// <summary>
        /// Creates strongly typed representation of <c>null</c>.
        /// </summary>
        /// <returns><see cref="ExpressionContainer{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Null(Type type)
        {
            return new ExpressionContainer(Expression.Convert(Expression.Constant(null), type));
        }
        
        /// <summary>
        /// Creates <see cref="TryExpression"/>.
        /// Parameters are resolved based on actual passed parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TryCatchFinallyBuilder Try()
        {
            return new TryCatchFinallyBuilder();
        }
        
        /// <summary>
        /// Executes code directly by passing <paramref name="code"/> to <see cref="Call"/>
        /// </summary>
        /// <param name="code"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer<T> Code<T>(Func<T> code)
        {
            return Call(() => code());
        }
        
        /// <summary>
        /// Executes code directly by passing <paramref name="code"/> to <see cref="Call"/>
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionContainer Code(Action code)
        {
            return Call(() => code());
        }

        /// <summary>
        /// Creates <see langword="switch"/> expression
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T">Type of variable</typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SwitchBuilder<T> Switch<T>(ExpressionContainer<T> value)
        {
            return new SwitchBuilder<T>(value);
        }

        /// <summary>
        /// Creates <see langword="if"/> expression
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConditionBuilder Condition(Type resultType = null)
        {
            return new ConditionBuilder(resultType);
        }
    }
}