using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Expressions.Shortcuts
{
    /// <summary>
    /// Shortcut for <see cref="BlockExpression"/>
    /// </summary>
    internal class BlockBuilder: ExpressionContainer
    {
        private readonly Type _returnType;
        private readonly List<Expression> _expressions;
        private readonly HashSet<ParameterExpression> _parameters;

        internal BlockBuilder(Type returnType) : base(Expression.Empty())
        {
            _returnType = returnType;
            _expressions = new List<Expression>();
            _parameters = new HashSet<ParameterExpression>();
        }

        /// <summary>
        /// Returns current block parameters
        /// </summary>
        public IEnumerable<ParameterExpression> Parameters => _parameters;
        
        /// <inheritdoc />
        public override Expression Expression => 
            _returnType == null 
                ? Expression.Block(_parameters, _expressions) 
                : Expression.Block(_returnType, _parameters, _expressions);

        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/>
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="expression"/> is not <see cref="ParameterExpression"/></exception>
        public BlockBuilder Parameter(Expression expression)
        {
            if (expression is ParameterExpression parameterExpression) return Parameter(parameterExpression);
                
            throw new ArgumentException("is not ParameterExpression", nameof(expression));
        }
        
        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Parameter<T>(out ExpressionContainer<T> parameter)
        {
            var expression = Expression.Parameter(typeof(T));
            parameter = ExpressionShortcuts.Arg<T>(expression);
            return Parameter(expression);
        }
        
        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Parameter<T>(string name, out ExpressionContainer<T> parameter)
        {
            var expression = Expression.Parameter(typeof(T), name);
            parameter = ExpressionShortcuts.Arg<T>(expression);
            return Parameter(expression);
        }
        
        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Parameter<T>(out ExpressionContainer<T> parameter, ExpressionContainer<T> value)
        {
            var expression = Expression.Parameter(typeof(T));
            parameter = ExpressionShortcuts.Arg<T>(expression);
            return Parameter(parameter, value);
        }
        
        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Parameter<T>(string name, out ExpressionContainer<T> parameter, ExpressionContainer<T> value)
        {
            var expression = Expression.Parameter(typeof(T), name);
            parameter = ExpressionShortcuts.Arg<T>(expression);
            return Parameter(parameter, value);
        }
        
        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Parameter<T>(out ExpressionContainer<T> parameter, T value)
        {
            var expression = Expression.Parameter(typeof(T));
            parameter = ExpressionShortcuts.Arg<T>(expression);
            return Parameter(parameter, ExpressionShortcuts.Arg(value));
        }
        
        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Parameter<T>(string name, out ExpressionContainer<T> parameter, T value)
        {
            var expression = Expression.Parameter(typeof(T), name);
            parameter = ExpressionShortcuts.Arg<T>(expression);
            return Parameter(parameter, ExpressionShortcuts.Arg(value));
        }

        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/> with initial assignment
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="expression"/> is not <see cref="ParameterExpression"/></exception>
        public BlockBuilder Parameter<TV>(ExpressionContainer<TV> expression, ExpressionContainer<TV> value)
        {
            if (!(expression.Expression is ParameterExpression parameterExpression))
                throw new ArgumentException("is not ParameterExpression", nameof(expression));
                
            _parameters.Add(parameterExpression);
            _expressions.Add(expression.Assign(value));
            return this;
        }
            
        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/> with initial assignment
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="expression"/> is not <see cref="ParameterExpression"/></exception>
        public BlockBuilder Parameter<TV>(ExpressionContainer<TV> expression, Expression value)
        {
            if (!(expression.Expression is ParameterExpression parameterExpression))
                throw new ArgumentException("is not ParameterExpression", nameof(expression));
                
            _parameters.Add(parameterExpression);
            _expressions.Add(expression.Assign(value));
            return this;
        }
            
        /// <summary>
        /// Adds parameter to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Parameter(ParameterExpression e)
        {
            _parameters.Add(e);
            return this;
        }
            
        /// <summary>
        /// Adds new "line" to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Line(Expression e)
        {
            _expressions.Add(e);
            return this;
        }

        /// <summary>
        /// Adds new "line" to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Line<TV>(ExpressionContainer<TV> e)
        {
            _expressions.Add(e);
            return this;
        }
            
        /// <summary>
        /// Adds multiple new "lines" to <see cref="BlockExpression"/>
        /// </summary>
        public BlockBuilder Lines(IEnumerable<Expression> e)
        {
            _expressions.AddRange(e);
            return this;
        }

        /// <summary>
        /// Creates <see cref="InvocationExpression"/> out of current <see cref="BlockExpression"/>.
        /// </summary>
        public ExpressionContainer<T> Invoke<T>(params ExpressionContainer[] parameters)
        {
            var lambda = Expression.Lambda(Expression, parameters.Select(o => (ParameterExpression) o.Expression));
            return ExpressionShortcuts.Arg<T>(Expression.Invoke(lambda));
        }
        
        /// <summary>
        /// Creates <see cref="InvocationExpression"/> out of current <see cref="BlockExpression"/>.
        /// </summary>
        public Expression<T> Lambda<T>(params ExpressionContainer[] parameters) where T: class
        {
            return Expression.Lambda<T>(Expression, parameters.Select(o => (ParameterExpression) o.Expression));
        }
    }
}