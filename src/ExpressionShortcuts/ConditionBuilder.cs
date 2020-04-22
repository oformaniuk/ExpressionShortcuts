using System;
using System.Linq.Expressions;

namespace Expressions.Shortcuts
{
    /// <summary>
    /// 
    /// </summary>
    internal class ConditionBuilder : ExpressionContainer
    {
        private Expression _condition;
        private Expression _then;
        private Expression _else;

        internal ConditionBuilder() : base(Expression.Empty())
        {
        }
        
        /// <summary>
        /// Adds <see langword="if"/> block
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder If(Expression condition, Expression then)
        {
            _condition = condition;
            _then = then;

            return this;
        }
        
        /// <summary>
        /// Adds <see langword="if"/> block
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder If(ExpressionContainer<bool> condition, Action<BlockBuilder> then)
        {
            _condition = condition;
            var block = new BlockBuilder(null);
            then(block);
            _then = block;
            
            return this;
        }

        /// <summary>
        /// Adds <see langword="else"/> block
        /// </summary>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder Else(Expression then)
        {
            _else = then;
            return this;
        }
        
        /// <summary>
        /// Adds <see langword="else"/> block
        /// </summary>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder Else(ExpressionContainer then)
        {
            _else = then;
            return this;
        }
        
        /// <summary>
        /// Adds <see langword="else"/> block
        /// </summary>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder Else(Action<BlockBuilder> then)
        {
            var block = new BlockBuilder(null);
            then(block);
            _else = block;
            
            return this;
        }

        /// <inheritdoc />
        public override Expression Expression
        {
            get
            {
                if(_condition == null) throw new InvalidOperationException("`if` statement is not defined");
                
                return _else == null 
                    ? Expression.IfThen(_condition, _then ?? Expression.Empty()) 
                    : Expression.IfThenElse(_condition, _then ?? Expression.Empty(), _else);
            }
        }
    }
}