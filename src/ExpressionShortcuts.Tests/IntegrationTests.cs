using System;
using System.Linq.Expressions;
using Bogus;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Expressions.Shortcuts.Tests
{
    public class IntegrationTests
    {
        private readonly IMock _mock;
        private readonly Faker _faker;

        public IntegrationTests()
        {
            _mock = Substitute.For<IMock>();
            _faker = new Faker();
        }
        
        [Fact]
        public void ClosureInCallTest()
        {
            var blockBuilder = ExpressionShortcuts.Block()
                .Line(ExpressionShortcuts.Call(() => _mock.MethodWithReturn()));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();

            _mock.Received(1).MethodWithReturn();
        }
        
        [Fact]
        public void MultipleCallsChainedTest()
        {
            _mock.MethodWithReturn().Returns(_faker.Random.String());
            
            var blockBuilder = ExpressionShortcuts.Block()
                .Line(ExpressionShortcuts.Call(() => _mock.MethodWithReturn().ToUpperInvariant()));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();

            _mock.Received(1).MethodWithReturn();
        }
        
        [Fact]
        public void NestedCallsTest()
        {
            var expected = _faker.Random.String();
            _mock.MethodWithReturn().Returns(expected);
            
            var blockBuilder = ExpressionShortcuts.Block()
                .Line(ExpressionShortcuts.Call(() => 
                    _mock.VoidMethodWithParameter(ExpressionShortcuts.Call(() => _mock.MethodWithReturn()))
                    )
                );

            Expression.Lambda<Action>(blockBuilder).Compile().Invoke();
            
            Received.InOrder(() =>
            {
                _mock.MethodWithReturn();
                _mock.VoidMethodWithParameter(expected);
            });
        }
        
        [Fact]
        public void NestedCallWithPropertyTest()
        {
            var expected = _faker.Random.String();
            _mock.String.Returns(expected);
            var mock = ExpressionShortcuts.Parameter<IMock>();

            Action action;

            action = ExpressionShortcuts.Block()
                .Parameter(mock)
                .Line(mock.Assign(_mock))
                .Line(mock.Call(o => o.VoidMethodWithParameter(o.String)))
                .Lambda<Action>()
                .Compile();

            _mock.DidNotReceiveWithAnyArgs().VoidMethodWithParameter(default);
            
            action.Invoke();

            _ = _mock.Received(1).String;
            _mock.Received(1).VoidMethodWithParameter(expected);
            
            _mock.ClearReceivedCalls();
            
            var mockVariable = Expression.Variable(typeof(IMock));
            var block = Expression.Block(new[] {mockVariable},
                Expression.Assign(mockVariable, Expression.Constant(_mock)),
                Expression.Call(
                    mockVariable,
                    typeof(IMock).GetMethod(nameof(IMock.VoidMethodWithParameter)),
                    Expression.Property(mockVariable, typeof(IMock).GetProperty(nameof(IMock.String)))
                )
            );
            
            Expression.Lambda<Action>(block).Compile().Invoke();
            
            _ = _mock.Received(1).String;
            _mock.Received(1).VoidMethodWithParameter(expected);
        }
        
        [Fact]
        public void CallChainedWithPropertyTest()
        {
            _mock.String.Returns(_faker.Random.String());
            
            var data = ExpressionShortcuts.Parameter<IMock>();
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Property(o => o.String).Call(o => o.ToUpperInvariant()));

            var actual = Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();

            _ = _mock.Received(1).String;
            var expected = _mock.String.ToUpperInvariant();
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void ConditionTrueTest()
        {
            _mock.String.Returns(_faker.Random.String());
            _mock.Condition.Returns(true);
            _mock.MethodWithReturn().Returns(_faker.Random.String());
            
            var data = ExpressionShortcuts.Parameter<IMock>();

            var action = ExpressionShortcuts.Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(ExpressionShortcuts.Condition()
                    .If(data.Property(o => o.Condition), data.Call(o => o.VoidMethodWithoutParameters()))
                    .Else(data.Call(o => o.VoidMethodWithParameter(o.String))))
                .Lambda<Action>()
                .Compile();

            _mock.DidNotReceiveWithAnyArgs().VoidMethodWithParameter(default);
            _mock.DidNotReceive().VoidMethodWithoutParameters();
            _ = _mock.DidNotReceiveWithAnyArgs().String;
            _ = _mock.DidNotReceiveWithAnyArgs().Condition;
            
            action.Invoke();

            _ = _mock.Received().Condition;
            _mock.Received(1).VoidMethodWithoutParameters();
        }
        
        [Fact]
        public void ConditionFalseTest()
        {
            _mock.String.Returns(_faker.Random.String());
            _mock.Condition.Returns(false);
            _mock.MethodWithReturn().Returns(_faker.Random.String());
            
            var data = ExpressionShortcuts.Parameter<IMock>();

            var action = ExpressionShortcuts.Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(ExpressionShortcuts.Condition()
                    .If(data.Property(o => o.Condition), data.Call(o => o.VoidMethodWithoutParameters()))
                    .Else(data.Call(o => o.VoidMethodWithParameter(o.String))))
                .Lambda<Action>()
                .Compile();

            _mock.DidNotReceiveWithAnyArgs().VoidMethodWithParameter(default);
            _mock.DidNotReceive().VoidMethodWithoutParameters();
            _ = _mock.DidNotReceiveWithAnyArgs().String;
            _ = _mock.DidNotReceiveWithAnyArgs().Condition;
            
            action.Invoke();

            _ = _mock.Received().Condition;
            _ = _mock.Received().String;
            _mock.Received().VoidMethodWithParameter(_mock.String);
        }
        
        [Fact]
        public void SwitchTest()
        {
            _mock.String.Returns(_faker.Random.String());
            _mock.Int.Returns(42);
            _mock.MethodWithReturn().Returns(_faker.Random.String());
            
            var data = ExpressionShortcuts.Parameter<IMock>();
            
            var @switch = ExpressionShortcuts.Switch(data.Property(o => o.Int))
                .Default(ExpressionShortcuts.Code(() => 0))
                .Case<int>((e, builder) =>
                {
                    builder.Line(data.Call(o => o.VoidMethodWithParameter(e.Call(x => x.ToString()))));
                    builder.Lines(e.Call(o => o).Return());
                }, ExpressionShortcuts.Arg(42));

            var action = ExpressionShortcuts.Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(@switch)
                .Lambda<Func<int>>()
                .Compile();

            _mock.DidNotReceiveWithAnyArgs().VoidMethodWithParameter(default);
            _ = _mock.DidNotReceiveWithAnyArgs().Int;
            
            var actual = action.Invoke();

            _ = _mock.Received(3).Int;
            var expected = _mock.Int;
            _mock.Received(1).VoidMethodWithParameter(expected.ToString());
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void StaticMethodCallTest()
        {
            var expected = _faker.Random.String();
            _mock.String.Returns(expected);
            var property = ExpressionShortcuts.Arg<string>(Expression.Constant(_mock.String));

            var mock = ExpressionShortcuts.Parameter<IMock>();
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(mock)
                .Line(mock.Assign(_mock))
                .Line(ExpressionShortcuts.Call(() => StaticMethod((IMock) mock, property)));

            Expression.Lambda<Action>(blockBuilder).Compile().Invoke();

            _ = _mock.Received(1).String;
            _mock.Received(1).VoidMethodWithParameter(expected);
        }

        private static void StaticMethod(IMock mock, string property)
        {
            mock.VoidMethodWithParameter(property);
        } 
        
        [Fact]
        public void CallVoidMethodWithParameter()
        {
            var expected = "newFistName";
            
            var data = ExpressionShortcuts.Parameter<IMock>();
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Call(o => o.VoidMethodWithParameter(expected)));

            Expression.Lambda<Action>(blockBuilder).Compile().Invoke();

            _mock.Received(1).VoidMethodWithParameter(expected);
        }
        
        [Fact]
        public void CallVoidMethodWithoutParametersTest()
        {
            var data = ExpressionShortcuts.Parameter<IMock>();
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Call(o => o.VoidMethodWithoutParameters()));

            Expression.Lambda<Action>(blockBuilder).Compile().Invoke();

            _mock.Received(1).VoidMethodWithoutParameters();
        }
        
        [Fact]
        public void UsingTest()
        {
            var parameter = ExpressionShortcuts.Parameter<IMock>();
            ExpressionShortcuts.Block()
                .Parameter(parameter)
                .Line(parameter.Assign(_mock))
                .Line(parameter.Using((o, block) =>
                {
                    block.Line(o.Call(x => x.VoidMethodWithoutParameters()));
                }))
                .Lambda<Action>()
                .Compile()
                .Invoke();
            
            Received.InOrder(() =>
            {
                _mock.VoidMethodWithoutParameters();
                _mock.Dispose(); 
            });
        }
        
        [Fact]
        public void TryCatchTest()
        {
            var expected = "bbb";
            _mock.MethodWithReturn().Throws(new InvalidOperationException("aaa"));
            
            var parameter = ExpressionShortcuts.Parameter<IMock>();
            var actual = ExpressionShortcuts.Block()
                .Parameter(parameter)
                .Line(parameter.Assign(_mock))
                .Line(ExpressionShortcuts.Try()
                    .Body(builder => builder.Line(parameter.Call(o => o.MethodWithReturn())))
                    .Catch<InvalidOperationException>((e, builder) =>
                    {
                        builder
                            .Line(parameter.Call(o => o.VoidMethodWithParameter(e.Property(x => x.Message))))
                            .Line(parameter.Call(o => o.Dispose()))
                            .Line(ExpressionShortcuts.Code(() => expected));
                    })
                )
                .Lambda<Func<string>>()
                .Compile()
                .Invoke();
            
            Assert.Equal(expected, actual);

            Received.InOrder(() =>
            {
                _mock.MethodWithReturn();
                _mock.VoidMethodWithParameter(new InvalidOperationException("aaa").Message);
                _mock.Dispose(); 
            });
        }

        [Fact]
        public void UsingWithThrowTest()
        {
            var expected = new InvalidOperationException();
            _mock.MethodWithReturn().Throws(expected);
            
            var mock = ExpressionShortcuts.Arg(_mock);
            var parameter = ExpressionShortcuts.Parameter<IMock>();
            var action = ExpressionShortcuts.Block()
                .Parameter(parameter, mock)
                .Line(parameter.Using((o, block) =>
                {
                    block.Line(o.Call(x => x.MethodWithReturn()));
                }))
                .Lambda<Action>()
                .Compile();

            var actual = Assert.Throws<InvalidOperationException>(() => action());
            Assert.Equal(expected, actual);
            Received.InOrder(() =>
            {
                _mock.MethodWithReturn();
                _mock.Dispose();
            });
        }
        
        [Fact]
        public void UsingWithReturnStatementTest()
        {
            var expected = _faker.Random.String();
            _mock.MethodWithReturn().Returns(expected);

            var mock = ExpressionShortcuts.Arg(_mock);
            var parameter = ExpressionShortcuts.Parameter<IMock>();
            var actual = ExpressionShortcuts.Block()
                .Parameter(parameter, mock)
                .Line(parameter.Using((o, block) =>
                {
                    block
                        .Line(o.Call(x => x.VoidMethodWithParameter("aaa")))
                        .Lines(o.Call(x => x.MethodWithReturn()).Return());
                }))
                .Lambda<Func<string>>()
                .Compile()
                .Invoke();
            
            Assert.Equal(expected, actual);
            Received.InOrder(() =>
            {
                _mock.VoidMethodWithParameter("aaa");
                _mock.MethodWithReturn();
                _mock.Dispose(); 
            });
        }
        
        [Fact]
        public void UsingWithReturnTest()
        {
            var expected = _faker.Random.String();
            _mock.MethodWithReturn().Returns(expected);
            
            var parameter = ExpressionShortcuts.Parameter<IMock>();
            var actual = ExpressionShortcuts.Block()
                .Parameter(parameter)
                .Line(parameter.Assign(_mock))
                .Line(parameter.Using((o, block) =>
                {
                    block.Line(o.Call(x => x.VoidMethodWithParameter("aaa")));
                    block.Line(o.Call(x => x.MethodWithReturn()));
                }))
                .Lambda<Func<string>>()
                .Compile()
                .Invoke();
            
            Assert.Equal(expected, actual);
            Received.InOrder(() =>
            {
                _mock.VoidMethodWithParameter("aaa");
                _mock.MethodWithReturn();
                _mock.Dispose(); 
            });
        }
        
        [Fact]
        public void IsWhenTypeMatchTest()
        {
            var data = ExpressionShortcuts.Parameter<IMock>();
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Is<IMock>());

            var result = Expression.Lambda<Func<bool>>(blockBuilder).Compile().Invoke();
            
            Assert.True(result);
        }
        
        [Fact]
        public void IsWhenTypeDoesNotMatchTest()
        {
            var data = ExpressionShortcuts.Parameter<IMock>();
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Is<string>());

            var result = Expression.Lambda<Func<bool>>(blockBuilder).Compile().Invoke();
            
            Assert.False(result);
        }
        
        [Fact]
        public void NewByTypeTest()
        {
            var data = ExpressionShortcuts.Parameter<object>();
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(data, ExpressionShortcuts.New<object>());

            var result = Expression.Lambda<Func<object>>(blockBuilder).Compile().Invoke();
            
            Assert.NotNull(result);
        }
        
        [Fact]
        public void NewByExpressionTest()
        {
            var data = ExpressionShortcuts.Parameter<string>();
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(data, ExpressionShortcuts.New(() => new string('a', 3)));

            var result = Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();
            
            Assert.Equal("aaa", result);
        }
        
        [Fact]
        public void ExternalArgPropertyTest()
        {
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(variable.Property(o => o.String));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();
            
            _ = _mock.Received(1).String;
        }
        
        [Fact]
        public void DeepPropertyTest()
        {
            var expected = _faker.Random.String();
            _mock.Self.Returns(_mock);
            _mock.String.Returns(expected);
            
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            var actual = ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(variable.Property(o => o.Self.Self.Self.String))
                .Lambda<Func<string>>()
                .Compile()
                .Invoke();
            
            Assert.Equal(expected, actual);
            
            _ = _mock.Received(3).Self;
            _ = _mock.Received(1).String;
        }
        
        [Fact]
        public void DeepCallTest()
        {
            _mock.Self.Returns(_mock);
            
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(variable.Call(o => o.Self.Self.Self.MethodWithReturn()))
                .Lambda<Func<string>>()
                .Compile()
                .Invoke();
            
            _ = _mock.Received(3).Self;
            _mock.Received(1).MethodWithReturn();
        }
        
        [Fact]
        public void CodeFuncTest()
        {
            _mock.Self.Returns(_mock);
            const int count = 10;
            
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(variable.Code(o =>
                {
                    var local = o;
                    for (var i = 0; i < count; i++)
                    {
                        local = local.Self;
                    }

                    return local.MethodWithReturn();
                }))
                .Lambda<Func<string>>()
                .Compile()
                .Invoke();
            
            _ = _mock.Received(count).Self;
            _mock.Received(1).MethodWithReturn();
        }
        
        [Fact]
        public void UnboundCodeFuncTest()
        {
            _mock.Self.Returns(_mock);
            const int count = 10;
            
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(ExpressionShortcuts.Code(() =>
                {
                    var local = _mock;
                    for (var i = 0; i < count; i++)
                    {
                        local = local.Self;
                    }

                    return local.MethodWithReturn();
                }))
                .Lambda<Func<string>>()
                .Compile()
                .Invoke();
            
            _ = _mock.Received(count).Self;
            _mock.Received(1).MethodWithReturn();
        }
        
        [Fact]
        public void CodeActionTest()
        {
            _mock.Self.Returns(_mock);
            const int count = 10;
            
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(variable.Code(o =>
                {
                    var local = o;
                    for (var i = 0; i < count; i++)
                    {
                        local = local.Self;
                    }

                    local.VoidMethodWithParameter("aaa");
                }))
                .Lambda<Action>()
                .Compile()
                .Invoke();

            _ = _mock.Received(count).Self;
            _mock.Received(1).VoidMethodWithParameter("aaa");
        }
        
        [Fact]
        public void UnboundCodeActionTest()
        {
            _mock.Self.Returns(_mock);
            const int count = 10;
            
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(ExpressionShortcuts.Code(() =>
                {
                    var local = _mock;
                    for (var i = 0; i < count; i++)
                    {
                        local = local.Self;
                    }

                    local.VoidMethodWithParameter("aaa");
                }))
                .Lambda<Action>()
                .Compile()
                .Invoke();
            
            _ = _mock.Received(count).Self;
            _mock.Received(1).VoidMethodWithParameter("aaa");
        }
        
        [Fact]
        public void PropertyByNameTest()
        {
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(ExpressionShortcuts.Property<string>(variable.Expression, nameof(IMock.String)));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();
            
            _ = _mock.Received(1).String;
        }
        
        [Fact]
        public void PropertyViaExpressionTest()
        {
            var arg = ExpressionShortcuts.Arg(_mock);
            var variable = ExpressionShortcuts.Var<IMock>();
            
            var blockBuilder = ExpressionShortcuts.Block()
                .Parameter(variable, arg)
                .Line(ExpressionShortcuts.Property<IMock, string>(variable.Expression, data => data.String));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();
            
            _ = _mock.Received(1).String;
        }
    }

    public interface IMock : IDisposable
    {
        string String { get; set; }
        int Int { get; set; }
        IMock Self { get; set; }
        bool Condition { get; set; }

        string MethodWithReturn();
        void VoidMethodWithParameter(string? value);
        void VoidMethodWithoutParameters();
    }
}