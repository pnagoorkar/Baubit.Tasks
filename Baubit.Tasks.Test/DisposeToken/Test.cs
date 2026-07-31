namespace Baubit.Tasks.Test.DisposeToken
{
    /// <summary>
    /// Tests for <see cref="Baubit.Tasks.DisposeToken"/>
    /// </summary>
    public class Test
    {
        [Fact]
        public void DisposeToken_Disposed_InitiallyFalse()
        {
            // Arrange & Act
            using var token = new Tasks.DisposeToken(() => { });

            // Assert
            Assert.False(token.Disposed);
        }

        [Fact]
        public void Dispose_InvokesCallback()
        {
            // Arrange
            var invoked = false;
            var token = new Tasks.DisposeToken(() => invoked = true);

            // Act
            token.Dispose();

            // Assert
            Assert.True(invoked);
        }

        [Fact]
        public void Dispose_SetsDisposedToTrue()
        {
            // Arrange
            var token = new Tasks.DisposeToken(() => { });

            // Act
            token.Dispose();

            // Assert
            Assert.True(token.Disposed);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_InvokesCallbackOnlyOnce()
        {
            // Arrange
            var invocationCount = 0;
            var token = new Tasks.DisposeToken(() => invocationCount++);

            // Act
            token.Dispose();
            token.Dispose();
            token.Dispose();

            // Assert
            Assert.Equal(1, invocationCount);
        }

        [Fact]
        public void Dispose_WithNullCallback_DoesNotThrow()
        {
            // Arrange
            var token = new Tasks.DisposeToken(null);

            // Act
            var exception = Record.Exception(() => token.Dispose());

            // Assert
            Assert.Null(exception);
            Assert.True(token.Disposed);
        }

        [Fact]
        public void Dispose_WithNullCallback_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var token = new Tasks.DisposeToken(null);

            // Act
            token.Dispose();
            var exception = Record.Exception(() => token.Dispose());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_ReleasesReferenceToCallback_AfterFirstDispose()
        {
            // Arrange
            var invocationCount = 0;
            Action callback = () => invocationCount++;
            var token = new Tasks.DisposeToken(callback);

            // Act
            token.Dispose();
            token.Dispose();

            // Assert - callback only invoked once even though Dispose called twice
            Assert.Equal(1, invocationCount);
        }

        [Fact]
        public void Dispose_UsedInUsingBlock_InvokesCallbackOnScopeExit()
        {
            // Arrange
            var invoked = false;

            // Act
            using (new Tasks.DisposeToken(() => invoked = true))
            {
                Assert.False(invoked);
            }

            // Assert
            Assert.True(invoked);
        }
    }
}
