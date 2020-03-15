using System;
using System.Linq.Expressions;

namespace Expressions.Shortcuts
{
    public partial class TryCatchFinallyBuilder
    {
        /// <summary>
        /// Adds <see langword="catch" /> statement catching all <see cref="Exception"/> types
        /// </summary>
        /// <param name="catch"></param>
        /// <param name="when"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TryCatchFinallyBuilder Catch<T>(Action<ExpressionContainer<T>, BlockBuilder> @catch, Func<ExpressionContainer<T>, ExpressionContainer<bool>> when) where T: Exception
        {
            var exception = ExpressionShortcuts.Var<T>();
            var body = ExpressionShortcuts.Block();
            @catch(exception, body);

            var filter = when?.Invoke(exception);
            var catchBlock = filter == null 
                ? Expression.Catch((ParameterExpression) exception, body) 
                : Expression.Catch((ParameterExpression) exception, body, filter);
            
            return Catch(catchBlock);
        }
        
        /// <summary>
        /// Adds <see langword="catch" /> statement catching all <see cref="Exception"/> types
        /// </summary>
        /// <param name="catch"></param>
        /// <param name="when"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TryCatchFinallyBuilder Catch<T>(Func<ExpressionContainer<T>, Expression> @catch, Func<ExpressionContainer<T>, ExpressionContainer<bool>> when) where T: Exception
        {
            var exception = ExpressionShortcuts.Var<T>();
            var body = @catch(exception);

            var filter = when?.Invoke(exception);
            var catchBlock = filter == null 
                ? Expression.Catch((ParameterExpression) exception, body) 
                : Expression.Catch((ParameterExpression) exception, body, filter);
            
            return Catch(catchBlock);
        }
    }
}