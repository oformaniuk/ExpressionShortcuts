// Prototyping extended expression trees for C#.
//
// bartde - October 2015

using System.Dynamic.Utils;
using System.Linq.Expressions;

namespace Microsoft.CSharp.Expressions
{
    partial class CSharpExpression
    {
        private static void ValidateCondition(Expression test, bool optionalTest = false)
        {
            if (optionalTest && test == null)
            {
                return;
            }

            ExpressionUtils.RequiresCanRead(test, nameof(test));

            // TODO: We can be more flexible and allow the rules in C# spec 7.20.
            //       Note that this behavior is the same as IfThen, but we could also add C# specific nodes for those,
            //       with the more flexible construction behavior.
            if (test.Type != typeof(bool))
            {
                throw LinqError.ArgumentMustBeBoolean();
            }
        }
    }
}
