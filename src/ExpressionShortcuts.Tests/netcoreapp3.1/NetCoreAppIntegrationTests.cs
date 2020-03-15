using System;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Expressions.Shortcuts.Tests
{
    public class NetCoreAppIntegrationTests
    {
        private readonly IMock _mock;

        public NetCoreAppIntegrationTests()
        {
            _mock = Substitute.For<IMock>();
        }
        
        [Fact]
        public void TryCatchWhenTest()
        {
            var expected = new InvalidOperationException("aaa");
            _mock.MethodWithReturn().Throws(expected);
            _mock.Condition.Returns(true);
            
            var parameter = ExpressionShortcuts.Parameter<IMock>();
            var action = ExpressionShortcuts.Block()
                .Parameter(parameter)
                .Line(parameter.Assign(_mock))
                .Line(ExpressionShortcuts.Try()
                    .Body(parameter.Call(o => o.MethodWithReturn()))
                    .Catch<InvalidOperationException>(
                        e => ExpressionShortcuts.Null<string>(), 
                        e => e.Call(o => !parameter.Property(x => x.Condition) || o.Message == null)
                    )
                )
                .Lambda<Func<string>>()
                .Compile();

            var actual = Assert.Throws<InvalidOperationException>(() => action());
            Assert.Equal(expected, actual);
            
            _mock.Received(1).MethodWithReturn();
            _ = _mock.Received(1).Condition;
        }
    }
}