using Avalonia.Controls;
using StringifyDesktop.Dialogs;
using StringifyDesktop.Infrastructure;

namespace StringifyDesktop.Tests;

public sealed class MainWindowCoordinatorTests
{
    [Fact]
    public void ShouldInterceptCloseRequest_ReturnsTrue_WhenWatcherPromptShouldBeShown()
    {
        var result = MainWindowCoordinator.ShouldInterceptCloseRequest(
            allowClose: false,
            isHandlingCloseRequest: false,
            closeReason: WindowCloseReason.WindowClosing,
            shouldOfferTrayOnClose: true);

        Assert.True(result);
    }

    [Fact]
    public void ShouldInterceptCloseRequest_ReturnsFalse_ForApplicationShutdown()
    {
        var result = MainWindowCoordinator.ShouldInterceptCloseRequest(
            allowClose: false,
            isHandlingCloseRequest: false,
            closeReason: WindowCloseReason.ApplicationShutdown,
            shouldOfferTrayOnClose: true);

        Assert.False(result);
    }

    [Theory]
    [InlineData(TrayPromptResult.Cancel, 0)]
    [InlineData(TrayPromptResult.MinimizeToTray, 1)]
    [InlineData(TrayPromptResult.Quit, 2)]
    public void ResolvePromptResult_MapsPromptChoiceToEffect(
        TrayPromptResult promptResult,
        int expected)
    {
        var effect = MainWindowCoordinator.ResolvePromptResult(promptResult);

        Assert.Equal(expected, (int)effect);
    }
}
