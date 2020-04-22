using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Expressions.Shortcuts
{
    /// <summary>
    /// 
    /// </summary>
    internal partial class TryCatchFinallyBuilder : ExpressionContainer
    {
        private readonly List<CatchBlock> _catchBlocks = new List<CatchBlock>();
        
        private Expression _finallyBody;
        private Expression _body;

        internal TryCatchFinallyBuilder() : base(Expression.Empty())
        {
        }

        /// <summary>
        /// Code surrounded by try/catch/finally
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public TryCatchFinallyBuilder Body(Action<BlockBuilder> body)
        {
            var blockBuilder = new BlockBuilder(null);
            body(blockBuilder);

            return Body(blockBuilder);
        }
        
        /// <summary>
        /// Code surrounded by try/catch/finally
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public TryCatchFinallyBuilder Body(Expression body)
        {
            _body = body;
            return this;
        }

        /// <summary>
        /// Adds <see langword="catch" /> statement catching <see cref="Exception"/> specified as <typeparamref name="T"/>
        /// </summary>
        /// <param name="catch"><see langword="catch" /> block body</param>
        /// <typeparam name="T"><see cref="Exception"/> type to catch</typeparam>
        /// <returns></returns>
        public TryCatchFinallyBuilder Catch<T>(Action<ExpressionContainer<T>, BlockBuilder> @catch) where T: Exception
        {
            var exception = ExpressionShortcuts.Var<T>();
            var body = ExpressionShortcuts.Block();
            @catch(exception, body);

            var catchBlock = Expression.Catch((ParameterExpression) exception, body);
            return Catch(catchBlock);
        }
        
        /// <summary>
        /// Adds <see langword="catch" /> statement catching <see cref="Exception"/> specified as <typeparamref name="T"/>
        /// </summary>
        /// <param name="catch"><see langword="catch" /> block body</param>
        /// <typeparam name="T"><see cref="Exception"/> type to catch</typeparam>
        /// <returns></returns>
        public TryCatchFinallyBuilder Catch<T>(Func<ExpressionContainer<T>, Expression> @catch) where T: Exception
        {
            var exception = ExpressionShortcuts.Var<T>();
            var body = @catch(exception);

            var catchBlock = Expression.Catch((ParameterExpression) exception, body);
            return Catch(catchBlock);
        }
        
        /// <summary>
        /// Adds <see langword="catch" /> statement catching all <see cref="Exception"/> types
        /// </summary>
        /// <param name="catch"><see langword="catch" /> block body</param>
        /// <returns></returns>
        public TryCatchFinallyBuilder Catch(CatchBlock @catch)
        {
            _catchBlocks.Add(@catch);

            return this;
        }
        
        /// <summary>
        /// Adds <see langword="finally" /> statement
        /// </summary>
        /// <param name="finally"><see langword="catch" /> block body</param>
        /// <returns></returns>
        public TryCatchFinallyBuilder Finally(Action<BlockBuilder> @finally)
        {
            var blockBuilder = ExpressionShortcuts.Block();
            @finally(blockBuilder);
            
            return Finally(blockBuilder);
        }
        
        /// <summary>
        /// Adds <see langword="finally" /> statement
        /// </summary>
        /// <param name="finally"><see langword="catch" /> block body</param>
        /// <returns></returns>
        public TryCatchFinallyBuilder Finally(Expression @finally)
        {
            _finallyBody = @finally;

            return this;
        }

        /// <inheritdoc cref="ExpressionContainer.Expression"/>
        public override Expression Expression
        {
            get
            {
                if (_finallyBody != null)
                {
                    return _catchBlocks.Any() 
                        ? Expression.TryCatchFinally(_body, _finallyBody, _catchBlocks.ToArray()) 
                        : Expression.TryFinally(_body, _finallyBody);
                }

                if(!_catchBlocks.Any()) throw new InvalidOperationException("No `catch` block provided");
                return Expression.TryCatch(_body, _catchBlocks.ToArray());
            }
        }
    }
}