using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Expressions.Shortcuts
{
    internal class ExpressionExtractorVisitor : ExpressionVisitor
    {
        public override Expression Visit(Expression node)
        {
            switch (node)
            {
                case LambdaExpression lambda:
                    return ExpressionUtils.ProcessCall(lambda.Body);
            }
            
            return base.Visit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var dynamicInvoke = Expression.Lambda(node).Compile().DynamicInvoke();
            return ConvertToExpression(dynamicInvoke, Visit) ?? Expression.Empty();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            switch (node.Expression)
            {
                case ConstantExpression constant:
                    var constantValue = constant.Value;
                    var value = constantValue.GetType().GetField(node.Member.Name)?.GetValue(constantValue);
                    if (value is ExpressionContainer) return ConvertToExpression(value, Visit) ?? Expression.Empty();
                    if (value?.GetType() == node.Type) return ConvertToExpression(value, Visit) ?? Expression.Empty();
                    
                    return Visit(Expression.Convert(Expression.Constant(value), node.Type)) ?? Expression.Empty();

                default: 
                    return base.VisitMember(node);
            }
        }

        protected override Expression VisitUnary(UnaryExpression unaryExpression)
        {
            switch (unaryExpression.NodeType)
            {
                case ExpressionType.ConvertChecked:
                case ExpressionType.Convert:
                {
                    if (!typeof(ExpressionContainer).IsAssignableFrom(unaryExpression.Operand.Type))
                        return unaryExpression.Type == unaryExpression.Operand.Type
                            ? unaryExpression.Update(ConvertToExpression(unaryExpression.Operand, Visit) ?? Expression.Empty())
                            : unaryExpression;
                    
                    var operand = ConvertToExpression(unaryExpression.Operand, Visit) ?? Expression.Empty();
                    if (operand.Type == typeof(void)) return operand;

                    if (typeof(ExpressionContainer).IsAssignableFrom(unaryExpression.Type))
                    {
                        return operand;
                    }
                    
                    return operand.Type != unaryExpression.Type 
                        ? Expression.Convert(operand, unaryExpression.Type) 
                        : operand;
                }

                default:
                    return unaryExpression.Update(ConvertToExpression(unaryExpression.Operand, Visit) ?? Expression.Empty());
            }
        }

        private static Expression? ConvertToExpression(object value, Func<Expression, Expression>? visit = null)
        {
            if (value is ExpressionContainer expressionContainer) return expressionContainer.Expression;
            if (value is Expression expression) return visit?.Invoke(expression);
            return Expression.Constant(value);
        }
    }
}