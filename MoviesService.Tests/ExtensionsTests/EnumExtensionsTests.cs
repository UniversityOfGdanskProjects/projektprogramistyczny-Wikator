using FluentAssertions;
using MoviesService.DataAccess.Extensions;
using MoviesService.Models.Enums;

namespace MoviesService.Tests.ExtensionsTests;

public class EnumExtensionsTests
{
    [Theory]
    [InlineData("Descending", "descending")]
    [InlineData("Ascending", "ascending")]
    public void ToCamelCaseString_ShouldReturnCorrectValue_OnSortOrder(string enumString, string expected)
    {
        // Arrange
        var genre = Enum.Parse<SortOrder>(enumString);

        // Act
        var result = genre.ToCamelCaseString();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Popularity", "popularity")]
    [InlineData("ReleaseDate", "releaseDate")]
    [InlineData("Title", "title")]
    [InlineData("AverageReviewScore", "averageReviewScore")]
    [InlineData("MinimumAge", "minimumAge")]
    public void ToCamelCaseString_ShouldReturnCorrectValue_OnSortBy(string enumString, string expected)
    {
        // Arrange
        var genre = Enum.Parse<SortBy>(enumString);

        // Act
        var result = genre.ToCamelCaseString();

        // Assert
        result.Should().Be(expected);
    }
}