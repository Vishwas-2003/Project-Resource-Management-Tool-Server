using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;

namespace Prm.Api.Tests.Services;

public class BackgroundJobServiceTests
{
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();

    [Fact]
    public void EnqueueSchedulerRun_ReturnsJobId()
    {
        const string expectedJobId = "job-123";
        _backgroundJobClient
            .Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns(expectedJobId);

        var sut = new BackgroundJobService(_backgroundJobClient.Object);
        var jobId = sut.EnqueueSchedulerRun();

        Assert.Equal(expectedJobId, jobId);
        _backgroundJobClient.Verify(
            x => x.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()),
            Times.Once);
    }
}
