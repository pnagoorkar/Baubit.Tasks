namespace Baubit.Tasks.Test.TypeExtensions
{
    public class Test
    {
        #region Test Helper Classes

        public class SimpleClass
        {
            public int Value { get; }

            public SimpleClass()
            {
                Value = 0;
            }

            public SimpleClass(int value)
            {
                Value = value;
            }
        }

        public class MultiParamClass
        {
            public string Name { get; }
            public int Age { get; }

            public MultiParamClass(string name, int age)
            {
                Name = name;
                Age = age;
            }
        }

        public interface ITestService
        {
            string GetValue();
        }

        public class TestService : ITestService
        {
            private readonly string _value;

            public TestService(string value)
            {
                _value = value;
            }

            public string GetValue() => _value;
        }

        public class ClassWithNullableParams
        {
            public string? Name { get; }
            public int? Count { get; }

            public ClassWithNullableParams(string? name, int? count)
            {
                Name = name;
                Count = count;
            }
        }

        public class ClassWithArrayParam
        {
            public int[] Values { get; }

            public ClassWithArrayParam(int[] values)
            {
                Values = values;
            }
        }

        #endregion

        #region CreateInstance Tests

        [Fact]
        public void CreateInstance_WithParameterlessConstructor_ReturnsInstance()
        {
            // Arrange
            var type = typeof(SimpleClass);

            // Act
            var result = type.CreateInstance<SimpleClass>(Array.Empty<Type>(), Array.Empty<object>());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(0, result.Value.Value);
        }

        [Fact]
        public void CreateInstance_WithSingleParameter_ReturnsInstance()
        {
            // Arrange
            var type = typeof(SimpleClass);

            // Act
            var result = type.CreateInstance<SimpleClass>(new[] { typeof(int) }, new object[] { 42 });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(42, result.Value.Value);
        }

        [Fact]
        public void CreateInstance_WithMultipleParameters_ReturnsInstance()
        {
            // Arrange
            var type = typeof(MultiParamClass);

            // Act
            var result = type.CreateInstance<MultiParamClass>(
                new[] { typeof(string), typeof(int) },
                new object[] { "John", 25 });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("John", result.Value.Name);
            Assert.Equal(25, result.Value.Age);
        }

        [Fact]
        public void CreateInstance_WithInterface_ReturnsInstanceAsInterface()
        {
            // Arrange
            var type = typeof(TestService);

            // Act
            var result = type.CreateInstance<ITestService>(
                new[] { typeof(string) },
                new object[] { "test value" });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("test value", result.Value.GetValue());
        }

        [Fact]
        public void CreateInstance_WithNonMatchingConstructor_ReturnsFailedResult()
        {
            // Arrange
            var type = typeof(SimpleClass);

            // Act - SimpleClass doesn't have a (string) constructor
            var result = type.CreateInstance<SimpleClass>(
                new[] { typeof(string) },
                new object[] { "invalid" });

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void CreateInstance_WithNullParameterValue_ReturnsInstance()
        {
            // Arrange
            var type = typeof(ClassWithNullableParams);

            // Act
            var result = type.CreateInstance<ClassWithNullableParams>(
                new[] { typeof(string), typeof(int?) },
                new object?[] { null, null });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Null(result.Value.Name);
            Assert.Null(result.Value.Count);
        }

        [Fact]
        public void CreateInstance_WithArrayParameter_ReturnsInstance()
        {
            // Arrange
            var type = typeof(ClassWithArrayParam);
            var values = new[] { 1, 2, 3 };

            // Act
            var result = type.CreateInstance<ClassWithArrayParam>(
                new[] { typeof(int[]) },
                new object[] { values });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(values, result.Value.Values);
        }

        [Fact]
        public void CreateInstance_WithEmptyArrays_AndParameterlessConstructor_ReturnsInstance()
        {
            // Arrange
            var type = typeof(SimpleClass);

            // Act
            var result = type.CreateInstance<SimpleClass>(
                new Type[] { },
                new object[] { });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void CreateInstance_WithIncorrectParameterTypes_ReturnsFailedResult()
        {
            // Arrange
            var type = typeof(MultiParamClass);

            // Act - Wrong order of parameters
            var result = type.CreateInstance<MultiParamClass>(
                new[] { typeof(int), typeof(string) },
                new object[] { 25, "John" });

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void CreateInstance_WithDerivedType_ReturnsInstanceAsBaseType()
        {
            // Arrange
            var type = typeof(TestService);

            // Act
            var result = type.CreateInstance<object>(
                new[] { typeof(string) },
                new object[] { "value" });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.IsType<TestService>(result.Value);
        }

        [Fact]
        public void CreateInstance_WithMismatchedParamCountTypes_ReturnsFailedResult()
        {
            // Arrange
            var type = typeof(SimpleClass);

            // Act - More values than types
            var result = type.CreateInstance<SimpleClass>(
                new[] { typeof(int) },
                new object[] { 42, "extra" });

            // Assert
            // The constructor lookup will find the int constructor, but invocation should work
            // Actually this might throw at Invoke - let's verify behavior
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void CreateInstance_WithWrongValueType_ReturnsFailedResult()
        {
            // Arrange
            var type = typeof(SimpleClass);

            // Act - Pass string instead of int
            var result = type.CreateInstance<SimpleClass>(
                new[] { typeof(int) },
                new object[] { "not an int" });

            // Assert
            Assert.True(result.IsFailed);
        }

        #endregion
    }
}
