using Microsoft.Extensions.Logging;
using Moq;

namespace SceneSplit.TestShared;

public static class TestHelper
{
    public static Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        var loggerMock = new Mock<ILogger<T>>();

        loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);

        return loggerMock;
    }

    public static Mock<ILogger> CreateLoggerMock()
    {
        var loggerMock = new Mock<ILogger>();

        loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);

        return loggerMock;
    }
}