using FluentResults;
using System;
using System.Reflection;

namespace Baubit.Tasks
{
    /// <summary>
    /// Provides extension methods for <see cref="Type"/> to enhance instance creation.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Creates an instance of the specified type using a constructor that matches the provided parameter types.
        /// </summary>
        /// <typeparam name="T">The type to cast the created instance to.</typeparam>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="paramTypes">The types of the constructor parameters.</param>
        /// <param name="paramValues">The values to pass to the constructor.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing the created instance on success, 
        /// or failure information if the constructor cannot be found or invocation fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method uses reflection to find a public instance constructor matching the specified 
        /// parameter types and invokes it with the provided values.
        /// </para>
        /// <para>
        /// The constructor must be public and have an exact match for the parameter types.
        /// Derived types in <paramref name="paramValues"/> are allowed, but <paramref name="paramTypes"/> 
        /// must exactly match the constructor signature.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = typeof(MyService).CreateInstance&lt;IMyService&gt;(
        ///     new[] { typeof(IConfiguration), typeof(ILogger) },
        ///     new object[] { config, logger }
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     var service = result.Value;
        /// }
        /// </code>
        /// </example>
        public static Result<T> CreateInstance<T>(this Type type, Type[] paramTypes, object[] paramValues)
        {
            return Result.Try(() =>
            {
                var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, paramTypes, null);
                return (T)ctor.Invoke(paramValues);
            });
        }
    }
}
