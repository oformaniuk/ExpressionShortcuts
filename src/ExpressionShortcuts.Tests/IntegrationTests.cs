#nullable enable
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Bogus;
using Microsoft.CSharp.Expressions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using static Expressions.Shortcuts.ExpressionShortcuts;

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
            var blockBuilder = Block()
                .Line(Call(() => _mock.MethodWithReturn()));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();

            _mock.Received(1).MethodWithReturn();
        }
        
        [Fact]
        public void FuncCallTest()
        {
            Func<IMock, string> func = m => m.MethodWithReturn();

            var expected = _faker.Random.String();
            _mock.MethodWithReturn().Returns(expected);
            var mock = Arg(_mock);
            var data = Var<IMock>();
            var action = Block()
                .Parameter(data, mock)
                .Line(Call(() => func((IMock)data)))
                .Lambda<Func<string>>()
                .Compile();

            _mock.DidNotReceive().MethodWithReturn();

            var actual = action();
            
            Assert.Equal(expected, actual);

            _mock.Received(1).MethodWithReturn();
        }
        
        [Fact]
        public void MultipleCallsChainedTest()
        {
            _mock.MethodWithReturn().Returns(_faker.Random.String());
            
            var blockBuilder = Block()
                .Line(Call(() => _mock.MethodWithReturn().ToUpperInvariant()));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();

            _mock.Received(1).MethodWithReturn();
        }
        
        [Fact]
        public void NestedCallsTest()
        {
            var expected = _faker.Random.String();
            _mock.MethodWithReturn().Returns(expected);
            
            var blockBuilder = Block()
                .Line(Call(() => 
                    _mock.VoidMethodWithParameter(Call(() => _mock.MethodWithReturn()))
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
            var mock = Parameter<IMock>();

            Action action;

            action = Block()
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
            
            var data = Parameter<IMock>();
            var blockBuilder = Block()
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
            
            var data = Parameter<IMock>();

            var action = Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(Condition()
                    .If(data.Property(o => o.Condition))
                    .Then(data.Call(o => o.VoidMethodWithoutParameters()))
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
            
            var action = Block()
                .Parameter(out var data, _mock)
                .Line(Condition()
                    .If(data.Property(o => o.Condition))
                    .Then(data.Call(o => o.VoidMethodWithoutParameters()))
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
        public void ConditionResultTest()
        {
            var expected = _faker.Random.String();
            _mock.Condition.Returns(true);
            _mock.MethodWithReturn().Returns(expected);
            
            var func = Block()
                .Parameter(out var data, _mock)
                .Line(Condition()
                    .If(data.Property(o => o.Condition))
                    .Then(data.Call(o => o.MethodWithReturn()))
                    .Else(data.Call(o => o.MethodWithReturn())))
                .Lambda<Func<string>>()
                .Compile();

            _mock.DidNotReceive().MethodWithReturn();
            _ = _mock.DidNotReceiveWithAnyArgs().Condition;

            var actual = func.Invoke();

            _ = _mock.Received().Condition;
            _mock.Received(1).MethodWithReturn();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void ConditionResultWithTypeTest()
        {
            var expected = _faker.Random.String();
            _mock.Condition.Returns(true);
            _mock.MethodWithReturn().Returns(expected);
            
            var func = Block()
                .Parameter(out var data, _mock)
                .Line(Condition(typeof(string))
                    .If(data.Property(o => o.Condition))
                    .Then(data.Call(o => o.MethodWithReturn()))
                    .Else(data.Call(o => o.MethodWithReturn()))
                )
                .Lambda<Func<string>>()
                .Compile();

            _mock.DidNotReceive().MethodWithReturn();
            _ = _mock.DidNotReceiveWithAnyArgs().Condition;

            var actual = func.Invoke();

            _ = _mock.Received().Condition;
            _mock.Received(1).MethodWithReturn();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void SwitchTest()
        {
            _mock.String.Returns(_faker.Random.String());
            _mock.Int.Returns(42);
            _mock.MethodWithReturn().Returns(_faker.Random.String());
            
            var data = Parameter<IMock>();
            
            var @switch = Switch(data.Property(o => o.Int))
                .Default(Code(() => 0))
                .Case<int>((e, builder) =>
                {
                    builder.Line(data.Call(o => o.VoidMethodWithParameter(e.Call(x => x.ToString()))));
                    builder.Lines(e.Call(o => o).Return());
                }, Arg(42));

            var action = Block()
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
            var property = Arg<string>(Expression.Constant(_mock.String));

            var mock = Parameter<IMock>();
            var blockBuilder = Block()
                .Parameter(mock)
                .Line(mock.Assign(_mock))
                .Line(Call(() => StaticMethod((IMock) mock, property)));

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
            
            var data = Parameter<IMock>();
            var blockBuilder = Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Call(o => o.VoidMethodWithParameter(expected)));

            Expression.Lambda<Action>(blockBuilder).Compile().Invoke();

            _mock.Received(1).VoidMethodWithParameter(expected);
        }
        
        [Fact]
        public void CallVoidMethodWithoutParametersTest()
        {
            var data = Parameter<IMock>();
            var blockBuilder = Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Call(o => o.VoidMethodWithoutParameters()));

            Expression.Lambda<Action>(blockBuilder).Compile().Invoke();

            _mock.Received(1).VoidMethodWithoutParameters();
        }
        
        [Fact]
        public void UsingTest()
        {
            var parameter = Parameter<IMock>();
            Expression<Action>? expression = (Expression<Action>?) Block()
                .Parameter(parameter)
                .Line(parameter.Assign(_mock))
                .Line(parameter.Using((o, block) =>
                {
                    block.Line(o.Call(x => x.VoidMethodWithoutParameters()));
                }))
                .Lambda<Action>()
                .Optimize();
            
            expression!.Compile().Invoke();
            
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
            
            var parameter = Parameter<IMock>();
            var actual = Block()
                .Parameter(parameter)
                .Line(parameter.Assign(_mock))
                .Line(Try()
                    .Body(builder => builder.Line(parameter.Call(o => o.MethodWithReturn())))
                    .Catch<InvalidOperationException>((e, builder) =>
                    {
                        builder
                            .Line(parameter.Call(o => o.VoidMethodWithParameter(e.Property(x => x.Message))))
                            .Line(parameter.Call(o => o.Dispose()))
                            .Line(Code(() => expected));
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
            
            var mock = Arg(_mock);
            var parameter = Parameter<IMock>();
            var action = Block()
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

            var mock = Arg(_mock);
            var parameter = Parameter<IMock>();
            var actual = Block()
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
            
            var parameter = Parameter<IMock>();
            var actual = Block()
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
            var data = Parameter<IMock>();
            var blockBuilder = Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Is<IMock>());

            var result = Expression.Lambda<Func<bool>>(blockBuilder).Compile().Invoke();
            
            Assert.True(result);
        }
        
        [Fact]
        public void IsWhenTypeDoesNotMatchTest()
        {
            var data = Parameter<IMock>();
            var blockBuilder = Block()
                .Parameter(data)
                .Line(data.Assign(_mock))
                .Line(data.Is<string>());

            var result = Expression.Lambda<Func<bool>>(blockBuilder).Compile().Invoke();
            
            Assert.False(result);
        }
        
        [Fact]
        public void NewByTypeTest()
        {
            var data = Parameter<object>();
            var blockBuilder = Block()
                .Parameter(data, New<object>());

            var result = Expression.Lambda<Func<object>>(blockBuilder).Compile().Invoke();
            
            Assert.NotNull(result);
        }
        
        [Fact]
        public void NewByExpressionTest()
        {
            var data = Parameter<string>();
            var blockBuilder = Block()
                .Parameter(data, New(() => new string('a', 3)));

            var result = Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();
            
            Assert.Equal("aaa", result);
        }
        
        [Fact]
        public void ExternalArgPropertyTest()
        {
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            var blockBuilder = Block()
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
            
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            var actual = Block()
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
            
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            Block()
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
            
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            Block()
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
            
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            Block()
                .Parameter(variable, arg)
                .Line(Code(() =>
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
            
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            Block()
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
            
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            Block()
                .Parameter(variable, arg)
                .Line(Code(() =>
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
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            var blockBuilder = Block()
                .Parameter(variable, arg)
                .Line(Property<string>(variable.Expression, nameof(IMock.String)));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();
            
            _ = _mock.Received(1).String;
        }
        
        [Fact]
        public void PropertyViaExpressionTest()
        {
            var arg = Arg(_mock);
            var variable = Var<IMock>();
            
            var blockBuilder = Block()
                .Parameter(variable, arg)
                .Line(Property<IMock, string>(variable.Expression, data => data.String));

            Expression.Lambda<Func<string>>(blockBuilder).Compile().Invoke();
            
            _ = _mock.Received(1).String;
        }
        
        [Fact]
        public void FieldByNameTest()
        {
            var expected = 42;
            var func = Block()
                .Parameter(out var variable, new MockWithField(expected))
                .Line(Field<int>(variable.Expression, nameof(MockWithField.Value)))
                .Lambda<Func<int>>()
                .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void FieldViaExpressionTest()
        {
            var expected = 42;
            var func = Block()
                    .Parameter(out var variable, new MockWithField(expected))
                    .Line(Field<MockWithField, int>(variable.Expression, data => data.Value))
                    .Lambda<Func<int>>()
                    .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void FieldLambdaTest()
        {
            var expected = 42;
            var func = Block()
                .Parameter(out var variable, new MockWithField(expected))
                .Line(variable.Field(o => o.Value))
                .Lambda<Func<int>>()
                .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void MemberByNameTest()
        {
            var expected = 42;
            var func = Block()
                .Parameter(out var variable, new MockWithField(expected))
                .Line(Member<int>(variable.Expression, nameof(MockWithField.Value)))
                .Lambda<Func<int>>()
                .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void MemberViaExpressionTest()
        {
            var expected = 42;
            var func = Block()
                .Parameter(out var variable, new MockWithField(expected))
                .Line(Member<MockWithField, int>(variable.Expression, data => data.Value))
                .Lambda<Func<int>>()
                .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void MemberLambdaTest()
        {
            var expected = 42;
            var func = Block()
                .Parameter(out var variable, new MockWithField(expected))
                .Line(variable.Member(o => o.Value))
                .Lambda<Func<int>>()
                .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void MemberPropertyByNameTest()
        {
            var expected = 42;
            var func = Block()
                .Parameter(out var variable, new MockWithField(expected))
                .Line(Member<int>(variable.Expression, nameof(MockWithField.ValueProperty)))
                .Lambda<Func<int>>()
                .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void MemberPropertyViaExpressionTest()
        {
            var expected = 42;
            var func = Block()
                .Parameter(out var variable, new MockWithField(expected))
                .Line(Member<MockWithField, int>(variable.Expression, data => data.ValueProperty))
                .Lambda<Func<int>>()
                .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void MemberPropertyLambdaTest()
        {
            var expected = 42;
            var func = Block()
                .Parameter(out var variable, new MockWithField(expected))
                .Line(variable.Member(o => o.ValueProperty))
                .Lambda<Func<int>>()
                .Compile();

            var actual = func();
            
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public async Task TestAsync()
        {
            var expected = 42;
            var func = Block()
                .Optimize()
                .Parameter(out var variable, new MockWithField(expected))
                .Parameter<int>(out var temp)
                .Parameter<int>(out var temp2)
                .Line(temp.Assign(variable.Member(o => o.ValueProperty).Call(o => CallAsync(o)).Await()))
                .Line(temp2.Assign(Await(() => GetValueAsync())))
                .Line(Call(() => (int)temp + (int)temp2))
                .AsyncLambda<Func<Task<int>>>()
                .Compile();

            var actual = await func();
            
            Assert.Equal(expected * 2, actual);
        }

        private static async Task<int> CallAsync(int value)
        {
            await Task.Delay(5).ConfigureAwait(false);
            return value;
        }
        
        private static async Task<int> GetValueAsync()
        {
            await Task.Delay(5).ConfigureAwait(false);
            return 42;
        }
    }

    public class MockWithField
    {
        public readonly int Value;
        
        public int ValueProperty { get; }

        public MockWithField(int value)
        {
            Value = value;
            ValueProperty = value;
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