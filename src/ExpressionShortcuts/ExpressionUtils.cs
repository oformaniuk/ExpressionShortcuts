using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Expressions.Shortcuts
{
    /// <summary>
    /// 
    /// </summary>
    public static class ExpressionUtils
    {
        /// <summary>
        /// Visits <paramref name="expressions"/> and replaces <see cref="ParameterExpression"/> by <paramref name="newValues"/> performing match by <see cref="Expression.Type"/>
        /// </summary>
        public static IEnumerable<Expression> ReplaceParameters(IEnumerable<Expression> expressions, IList<Expression> newValues)
        {
            return newValues.Count != 0 
                ? PerformReplacement() 
                : expressions;

            IEnumerable<Expression> PerformReplacement()
            {
                var visitor = new ParameterReplacerVisitor(newValues);
                return expressions.Where(o => o != null).Select(expression => visitor.Visit(expression));
            }
        }
        
        /// <summary>
        /// Visits <paramref name="expression"/> and replaces <see cref="ParameterExpression"/> by <paramref name="newValues"/> performing match by <see cref="Expression.Type"/>
        /// </summary>
        public static Expression ReplaceParameters(Expression expression, params Expression?[] newValues)
        {
            var visitor = new ParameterReplacerVisitor(newValues);
            return visitor.Visit(expression);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Expression ProcessPropertyLambda(Expression instance, LambdaExpression propertyLambda)
        {
            var member = propertyLambda.Body as MemberExpression;
            if (member == null) throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null) throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

            return ReplaceParameters(ExtractArgument(member), instance);
        }

        internal static Expression ProcessCallLambda(LambdaExpression propertyLambda, Expression? instance = null)
        {
            return ProcessCall(propertyLambda.Body, instance);
        }
        
        internal static Expression ProcessCall(Expression propertyLambda, Expression? instance = null)
        {
            switch (propertyLambda)
            {
                case NewExpression newExpression:
                    return Expression.New(newExpression.Constructor, ExtractArguments(newExpression.Arguments));

                case MethodCallExpression member:
                    var methodInfo = member.Method;
                    var parameters = instance != null ? new[] { instance } : new Expression[0];
                    instance = ReplaceParameters(new[] {member.Object}, parameters).SingleOrDefault();
                    IEnumerable<Expression> methodCallArguments = member.Arguments;
                    methodCallArguments = ReplaceParameters(methodCallArguments, parameters).Select(ExtractArgument);
                    var memberObject = methodInfo.IsStatic 
                        ? null : Expression.Convert(instance, methodInfo.DeclaringType);

                    return Expression.Call(memberObject, methodInfo, methodCallArguments);

                case InvocationExpression invocationExpression:
                    return invocationExpression.Update(
                        invocationExpression.Expression,
                        ExtractArguments(invocationExpression.Arguments)
                    );

                default:
                    return ReplaceParameters(ExtractArgument(propertyLambda), instance);
            }
        }

        internal static IReadOnlyCollection<Expression> ExtractArguments(IReadOnlyCollection<Expression> expressions)
        {
            var result = new Expression[expressions.Count];
            if (expressions is IList<Expression> list)
            {
                for (var index = 0; index < list.Count; index++)
                {
                    result[index] = ExtractArgument(list[index]);
                }

                return result;
            }
            else
            {
                int index = 0;
                foreach (var expr in expressions)
                {
                    result[index++] = ExtractArgument(expr);
                }
            }

            return result;
        }
        
        internal static Expression ExtractArgument(Expression expr)
        {
            return ExtractFromExpression(expr);
        }

        private static readonly ExpressionExtractorVisitor ExtractorVisitor = new ExpressionExtractorVisitor();
        private static Expression ExtractFromExpression(Expression expression)
        {
            return ExtractorVisitor.Visit(expression);
            // while (true)
            // {
            //     switch (expression)
            //     {
            //         case MethodCallExpression methodCall:
            //             return Expression.Lambda(methodCall).Compile().DynamicInvoke();
            //         
            //         case LambdaExpression lambda:
            //             return ProcessCallLambda(lambda);
            //         
            //         case MemberExpression memberExpression:
            //             switch (memberExpression.Expression)
            //             {
            //                 case ConstantExpression constant:
            //                     var constantValue = constant.Value;
            //                     var value = constantValue.GetType().GetField(memberExpression.Member.Name)?.GetValue(constantValue);
            //                     if (value is ExpressionContainer) return value;
            //                     return Expression.Convert(Expression.Constant(value), memberExpression.Type);
            //
            //                 default: 
            //                     return memberExpression;
            //             }
            //
            //         case UnaryExpression unaryExpression:
            //             switch (unaryExpression.NodeType)
            //             {
            //                 case ExpressionType.ConvertChecked:
            //                 case ExpressionType.Convert:
            //                     if (typeof(ExpressionContainer).IsAssignableFrom(unaryExpression.Operand.Type))
            //                     {
            //                         var operand = ExtractArgument(unaryExpression.Operand);
            //                         if (operand.Type != unaryExpression.Type)
            //                         {
            //                             return Expression.Convert(operand, unaryExpression.Type);
            //                         }
            //
            //                         if (operand.NodeType == ExpressionType.Call || operand.NodeType == ExpressionType.Invoke || operand.NodeType == ExpressionType.Lambda)
            //                         {
            //                             return operand;
            //                         }
            //
            //                         expression = operand;
            //                         continue;
            //                     }
            //                     
            //                     if (unaryExpression.Type != unaryExpression.Operand.Type)
            //                     {
            //                         return expression;
            //                     }
            //                     
            //                     expression = unaryExpression.Operand;
            //                     continue;
            //
            //                 default:
            //                     return unaryExpression.Update(ExtractArgument(unaryExpression.Operand));
            //             }
            //
            //         case BinaryExpression binaryExpression:
            //         {
            //             var left = ExtractArgument(binaryExpression.Left);
            //             var right = ExtractArgument(binaryExpression.Right);
            //
            //             return binaryExpression.Update(left, binaryExpression.Conversion, right);
            //         }
            //         
            //         default:
            //             return expression;
            //     }
            // }
        }
    }
}