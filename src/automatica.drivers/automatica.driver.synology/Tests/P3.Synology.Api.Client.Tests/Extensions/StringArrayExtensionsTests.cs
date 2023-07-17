using FluentAssertions;
using P3.Synology.Api.Client.Extensions;
using Xunit;

namespace P3.Synology.Api.Client.Tests.Extensions
{
    public class StringArrayExtensionsTests
    {
        [Fact]
        public void StringArrayExtensions_ValidArray_Success()
        {
            // arrange
            var words = new[] { "Hello", "World" };

            // act
            var result = words.ToCommaSeparatedAroundBrackets();

            // assert
            result.Should().BeEquivalentTo(@"[""Hello"",""World""]");
        }

        [Fact]
        public void StringArrayExtensions_EmptyArray_ReturnEmptyBrackets()
        {
            // arrange
            var words = System.Array.Empty<string>();

            // act
            var result = words.ToCommaSeparatedAroundBrackets();

            // assert
            result.Should().BeEquivalentTo("[]");
        }
    }
}
