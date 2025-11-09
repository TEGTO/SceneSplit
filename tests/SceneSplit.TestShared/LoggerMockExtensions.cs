using Microsoft.Extensions.Logging;
using Moq;

namespace SceneSplit.TestShared;

public static class LoggerMockExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> mock, LogLevel level, Times times, string eventName)
    {
        mock.Verify(x =>
            x.Log(
                level,
                It.Is<EventId>(x => x.Name == eventName),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    public static void VerifyLog(this Mock<ILogger> mock, LogLevel level, Times times, string eventName)
    {
        mock.Verify(x =>
            x.Log(
                level,
                It.Is<EventId>(x => x.Name == eventName),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}