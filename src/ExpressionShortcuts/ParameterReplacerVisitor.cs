using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Expressions.Shortcuts
{
    internal class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ICollection<Expression?> _replacements;
        private readonly bool _addIfMiss;

        public ParameterReplacerVisitor(IEnumerable<Expression?> replacements, bool addIfMiss = false)
        {
            _replacements = replacements.Where(o => o != null).ToList();
            _addIfMiss = addIfMiss;
        }
            
        protected override Expression VisitParameter(ParameterExpression node)
        {
            var replacement = _replacements.FirstOrDefault(o => o?.Type == node.Type);
            if (replacement == null || replacement == node)
            {
                if (_addIfMiss)
                {
                    _replacements.Add(node);
                }
                return base.VisitParameter(node);
            }
            
            return base.Visit(replacement);
        }
    }
}