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
        /// <returns></returns>
        public ConditionBuilder If(Expression condition)
        {
            _condition = condition;

            return this;
        }
        
        /// <summary>
        /// Adds <see langword="if"/> block
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public ConditionBuilder If(ExpressionContainer<bool> condition)
        {
            _condition = condition;
            
            return this;
        }

        /// <summary>
        /// Occurs in case <see cref="If(System.Linq.Expressions.Expression)"/> condition evaluated to <c>true</c>
        /// </summary>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder Then(Expression then)
        {
            _then = then;

            return this;
        }
        
        /// <summary>
        /// Occurs in case <see cref="If(System.Linq.Expressions.Expression)"/> condition evaluated to <c>true</c>
        /// </summary>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder Then(ExpressionContainer then)
        {
            _then = then.Expression;

            return this;
        }
        
        /// <summary>
        /// Occurs in case <see cref="If(System.Linq.Expressions.Expression)"/> condition evaluated to <c>true</c>
        /// </summary>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder Then<T>(ExpressionContainer<T> then)
        {
            _then = then.Expression;

            return this;
        }
        
        /// <summary>
        /// Occurs in case <see cref="If(System.Linq.Expressions.Expression)"/> condition evaluated to <c>true</c>
        /// </summary>
        /// <param name="then"></param>
        /// <returns></returns>
        public ConditionBuilder Then(Action<BlockBuilder> then)
        {
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
        public ConditionBuilder Else<T>(ExpressionContainer<T> then)
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