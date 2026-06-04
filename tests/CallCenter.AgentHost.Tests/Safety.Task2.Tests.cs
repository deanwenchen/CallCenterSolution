using CallCenter.Framework.Safety;

namespace CallCenter.AgentHost.Tests;

/// <summary>
/// Task 2 TDD RED: Failing tests for configurable KeywordFilter via SafetyOptions.
/// </summary>
public class SafetyTask2Tests
{
    [Fact]
    public void SafetyOptions_DefaultKeywords_MatchCurrentHardcoded()
    {
        // Arrange & Act
        var options = new SafetyOptions();

        // Assert — will FAIL until SafetyOptions class is created
        Assert.True(options.EnableKeywordFilter);
        Assert.True(options.EnableInjectionDetection);
        Assert.Contains("投诉", options.BlockedKeywords);
        Assert.Contains("举报", options.BlockedKeywords);
    }

    [Fact]
    public void KeywordFilter_Instance_UsesConfiguredKeywords()
    {
        // Arrange
        var options = new SafetyOptions { BlockedKeywords = new[] { "投诉", "举报" } };
        var filter = new KeywordFilter(options);

        // Act & Assert
        Assert.True(filter.IsBlocked("我要投诉"));
        Assert.False(filter.IsBlocked("正常订单查询"));
    }

    [Fact]
    public void KeywordFilter_Instance_EmptyKeywords_PassesAll()
    {
        // Arrange
        var options = new SafetyOptions { BlockedKeywords = Array.Empty<string>() };
        var filter = new KeywordFilter(options);

        // Act & Assert
        Assert.False(filter.IsBlocked("anything"));
    }

    [Fact]
    public void KeywordFilter_Static_BackwardsCompatible()
    {
        // Arrange & Act — static API should still work
        // Assert
        Assert.True(KeywordFilter.IsBlocked("我要投诉"));
        Assert.False(KeywordFilter.IsBlocked("正常订单查询"));
    }

    [Fact]
    public void KeywordFilter_GetBlockedKeyword_ReturnsFirstMatch()
    {
        // Arrange
        var options = new SafetyOptions { BlockedKeywords = new[] { "测试" } };
        var filter = new KeywordFilter(options);

        // Act
        var keyword = filter.GetBlockedKeyword("包含测试词");

        // Assert
        Assert.Equal("测试", keyword);
    }
}
