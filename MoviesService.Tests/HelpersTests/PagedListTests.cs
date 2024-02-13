using FluentAssertions;
using MoviesService.DataAccess.Helpers;

namespace MoviesService.Tests.HelpersTests;

public class PagedListTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void PagedList_ShouldReturnCorrectValues_WhenLastPageIsNotFull(int currentPage)
    {
        // Arrange
        var items = new List<string> { "a", "b" };
        const int pageSize = 2;
        const int totalCount = 7;

        // Act
        var pagedList = new PagedList<string>(items, currentPage, pageSize, totalCount);

        // Assert
        pagedList.Should().BeOfType<PagedList<string>>();
        pagedList.Items.Should().HaveCount(2);
        pagedList.CurrentPage.Should().Be(currentPage);
        pagedList.PageSize.Should().Be(pageSize);
        pagedList.TotalCount.Should().Be(totalCount);
        pagedList.TotalPages.Should().Be(4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PagedList_ShouldReturnCorrectValues_WhenLastPageIsFull(int currentPage)
    {
        // Arrange
        var items = new List<string> { "a", "b" };
        const int pageSize = 2;
        const int totalCount = 8;

        // Act
        var pagedList = new PagedList<string>(items, currentPage, pageSize, totalCount);

        // Assert
        pagedList.Should().BeOfType<PagedList<string>>();
        pagedList.Items.Should().HaveCount(2);
        pagedList.CurrentPage.Should().Be(currentPage);
        pagedList.PageSize.Should().Be(pageSize);
        pagedList.TotalCount.Should().Be(totalCount);
        pagedList.TotalPages.Should().Be(4);
    }
}