using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Expressions.Shortcuts
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SwitchBuilder<T> : ExpressionContainer
    {
        /// <summary>
        /// 
        /// </summary>
        protected Expression DefaultCase;
        
        /// <summary>
        /// 
        /// </summary>
        protected List<SwitchCase> Cases { get; set; } = new List<SwitchCase>();
        
        /// <summary>
        /// 
        /// </summary>
        protected ExpressionContainer<T> Value { get; }
        
        /// <summary>
        /// 
        /// </summary>
        protected MethodInfo ComparerMethod { get; set; }

        internal SwitchBuilder(ExpressionContainer<T> value) : base(Expression.Empty())
        {
            Value = value;
        }

        /// <summary>
        /// Creates <see langword="default"/> case
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="TR"></typeparam>
        /// <returns></returns>
        public SwitchBuilder<T, TR> Default<TR>(ExpressionContainer<TR> expression)
        {
            var switchBuilder = new SwitchBuilder<T, TR>(Value)
            {
                Cases = Cases,
                ComparerMethod = ComparerMethod,
                DefaultCase = DefaultCase
            };
            
            return switchBuilder.Default(expression);
        }

        /// <summary>
        /// Creates <see langword="default"/> case
        /// </summary>
        /// <param name="builder"></param>
        /// <typeparam name="TR"></typeparam>
        /// <returns></returns>
        public SwitchBuilder<T, TR> Default<TR>(Action<ExpressionContainer<T>, BlockBuilder> builder)
        {
            var switchBuilder = new SwitchBuilder<T, TR>(Value)
            {
                Cases = Cases,
                ComparerMethod = ComparerMethod,
                DefaultCase = DefaultCase
            };
            
            return switchBuilder.Default(builder);
        }
        
        /// <summary>
        /// Creates <see langword="default"/> case
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public SwitchBuilder<T> Default(ExpressionContainer expression)
        {
            DefaultCase = expression;
            return this;
        }
        
        /// <summary>
        /// Creates <see langword="default"/> <see langword="case"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public SwitchBuilder<T> Default(Action<ExpressionContainer<T>, BlockBuilder> builder)
        {
            var blockBuilder = new BlockBuilder(typeof(void));
            builder(Value, blockBuilder);
            DefaultCase = blockBuilder;
            
            return this;
        }
        
        /// <summary>
        /// Creates new <see langword="case"/> expression for values specified in <paramref name="testValues"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="testValues"></param>
        /// <typeparam name="TR"></typeparam>
        /// <returns></returns>
        public SwitchBuilder<T, TR> Case<TR>(ExpressionContainer<TR> expression, params ExpressionContainer<T>[] testValues)
        {
            var switchBuilder = new SwitchBuilder<T, TR>(Value)
            {
                Cases = Cases,
                ComparerMethod = ComparerMethod,
                DefaultCase = DefaultCase
            };
            
            return switchBuilder.Case(expression, testValues);
        }
        
        /// <summary>
        /// Creates new <see langword="case"/> expression for values specified in <paramref name="testValues"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="testValues"></param>
        /// <typeparam name="TR"></typeparam>
        /// <returns></returns>
        public SwitchBuilder<T, TR> Case<TR>(Action<ExpressionContainer<T>, BlockBuilder> builder, params ExpressionContainer<T>[] testValues)
        {
            var switchBuilder = new SwitchBuilder<T, TR>(Value)
            {
                Cases = Cases,
                ComparerMethod = ComparerMethod,
                DefaultCase = DefaultCase
            };
            
            return switchBuilder.Case(builder, testValues);
        }
        
        /// <summary>
        /// Creates new <see langword="case"/> expression for values specified in <paramref name="testValues"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="testValues"></param>
        /// <returns></returns>
        public SwitchBuilder<T> Case(ExpressionContainer expression, params ExpressionContainer<T>[] testValues)
        {
            Cases.Add(Expression.SwitchCase(expression, testValues.Select(o => o.Expression)));
            
            return this;
        }
        
        /// <summary>
        /// Creates new <see langword="case"/> expression for values specified in <paramref name="testValues"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="testValues"></param>
        /// <returns></returns>
        public SwitchBuilder<T> Case(Action<ExpressionContainer<T>, BlockBuilder> builder, params ExpressionContainer<T>[] testValues)
        {
            var blockBuilder = new BlockBuilder(typeof(void));
            builder(Value, blockBuilder);
            Cases.Add(Expression.SwitchCase(blockBuilder, testValues.Select(o => o.Expression)));

            return this;
        }
        
        /// <summary>
        /// Method used for comparison. <c>Must be <see langword="static"/> and accept 2 arguments of switch variable type</c>
        /// </summary>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public SwitchBuilder<T> Comparer(MethodInfo comparer)
        {
            if(!comparer.IsStatic) throw new ArgumentException("Method should be static", nameof(comparer));
            var parameters = comparer.GetParameters();
            if(parameters.Length != 2) throw new ArgumentException("Method should accept to parameters", nameof(comparer));
            if(parameters.All(o => o.ParameterType == typeof(T))) throw new ArgumentException("Method should accept to parameters", nameof(comparer));
            
            ComparerMethod = comparer;

            return this;
        }

        /// /// <inheritdoc />
        public override Expression Expression
        {
            get
            {
                if (DefaultCase != null)
                {
                    return ComparerMethod != null 
                        ? Expression.Switch(Value, DefaultCase, ComparerMethod, Cases) 
                        : Expression.Switch(Value, DefaultCase, Cases.ToArray());
                }
                
                return ComparerMethod != null 
                    ? Expression.Switch(Value, Expression.Empty(), ComparerMethod, Cases)
                    : Expression.Switch(Value, Cases.ToArray());
            }
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TR"></typeparam>
    public class SwitchBuilder<T, TR> : SwitchBuilder<T>
    {
        internal SwitchBuilder(ExpressionContainer<T> value) : base(value)
        {
            
        }
        
        /// <summary>
        /// Creates <see langword="default"/> case
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public SwitchBuilder<T, TR> Default(ExpressionContainer<TR> expression)
        {
            DefaultCase = expression;
            return this;
        }
        
        /// <summary>
        /// Creates <see langword="default"/> case
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public new SwitchBuilder<T, TR> Default(Action<ExpressionContainer<T>, BlockBuilder> builder)
        {
            var blockBuilder = new BlockBuilder(typeof(TR));
            builder(Value, blockBuilder);
            DefaultCase = blockBuilder;
            
            return this;
        }
        
        /// <summary>
        /// Creates new <see langword="case"/> expression for values specified in <paramref name="testValues"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="testValues"></param>
        /// <returns></returns>
        public SwitchBuilder<T, TR> Case(ExpressionContainer<TR> expression, params ExpressionContainer<T>[] testValues)
        {
            Cases.Add(Expression.SwitchCase(expression, testValues.Select(o => o.Expression)));
            
            return this;
        }
        
        /// <summary>
        /// Creates new <see langword="case"/> expression for values specified in <paramref name="testValues"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="testValues"></param>
        /// <returns></returns>
        public new SwitchBuilder<T, TR> Case(Action<ExpressionContainer<T>, BlockBuilder> builder, params ExpressionContainer<T>[] testValues)
        {
            var blockBuilder = new BlockBuilder(typeof(TR));
            builder(Value, blockBuilder);
            Cases.Add(Expression.SwitchCase(blockBuilder, testValues.Select(o => o.Expression)));

            return this;
        }

        /// <summary>
        /// Method used for comparison. <c>Must be <see langword="static"/> and accept 2 arguments of switch variable type</c>
        /// </summary>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public new SwitchBuilder<T, TR> Comparer(MethodInfo comparer)
        {
            if(!comparer.IsStatic) throw new ArgumentException("Method should be static", nameof(comparer));
            var parameters = comparer.GetParameters();
            if(parameters.Length != 2) throw new ArgumentException("Method should accept to parameters", nameof(comparer));
            if(parameters.Any(o => o.ParameterType != typeof(T))) throw new ArgumentException("Method should accept to parameters", nameof(comparer));
            
            ComparerMethod = comparer;

            return this;
        }
    }
}