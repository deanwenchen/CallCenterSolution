using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace CallCenter.AgentHost.Tests;

/// <summary>
/// ProcessStreamingAsync + DriveStreamingAsync + SSE 序列化测试。
/// TDD RED→GREEN→REFACTOR cycle.
/// </summary>
public class CallCenterServiceStreamingTests
{
    // ===== SerializeEventSse Tests =====

    [Fact]
    public void SerializeEventSse_WorkflowStarted_ProducesCorrectSseFormat()
    {
        // Arrange
        var evt = new WorkflowStartedEvent();

        // Act
        var sse = CallCenterService.SerializeEventSse(evt);

        // Assert
        Assert.StartsWith("data: {\"type\":\"WorkflowStarted\"", sse);
        Assert.EndsWith("\n\n", sse);
    }

    [Fact]
    public void SerializeEventSse_All9Types_CorrectTypeNames()
    {
        // Arrange — all 9 WorkflowEvent types per plan
        WorkflowEvent[] events =
        [
            new WorkflowStartedEvent(),
            CreateExecutorInvokedEvent(),
            CreateExecutorCompletedEvent(),
            CreateRequestInfoEvent(),
            CreateWorkflowOutputEvent(),
            CreateWorkflowErrorEvent(),
            CreateExecutorFailedEvent(),
            CreateSuperStepCompletedEvent(),
            CreateWorkflowWarningEvent(),
        ];
        string[] expectedTypeNames =
        [
            "WorkflowStarted",
            "ExecutorInvoked",
            "ExecutorCompleted",
            "RequestInfo",
            "WorkflowOutput",
            "WorkflowError",
            "ExecutorFailed",
            "SuperStepCompleted",
            "WorkflowWarning",
        ];

        // Act & Assert
        for (int i = 0; i < events.Length; i++)
        {
            var sse = CallCenterService.SerializeEventSse(events[i]);
            Assert.Contains($"\"type\":\"{expectedTypeNames[i]}\"", sse);
            Assert.StartsWith("data: {", sse);
            Assert.EndsWith("\n\n", sse);
        }
    }

    [Fact]
    public void SerializeEventSse_NoEnvelope_DirectDataFormat()
    {
        // Arrange
        var evt = new WorkflowStartedEvent();

        // Act
        var sse = CallCenterService.SerializeEventSse(evt);

        // Assert — output matches data: {...} pattern without extra wrapper
        Assert.Matches(@"^data: \{""type"":""\w+"",""data"":\{.*\}\}\n\n$", sse);
    }

    // ===== Helper methods to create concrete event instances =====

    private static ExecutorInvokedEvent CreateExecutorInvokedEvent()
        => new("TestExecutor", "test message");

    private static ExecutorCompletedEvent CreateExecutorCompletedEvent()
        => new("TestExecutor", "result");

    private static RequestInfoEvent CreateRequestInfoEvent()
    {
        var portInfo = new RequestPortInfo(
            new TypeId(typeof(string)),
            new TypeId(typeof(string)),
            "TestPort");
        var requestData = new PortableValue("test data");
        var externalRequest = new ExternalRequest(portInfo, "req-1", requestData);
        return new RequestInfoEvent(externalRequest);
    }

    private static WorkflowOutputEvent CreateWorkflowOutputEvent()
        => new("output data", "testExecutor");

    private static WorkflowErrorEvent CreateWorkflowErrorEvent()
        => new(new Exception("test error"));

    private static ExecutorFailedEvent CreateExecutorFailedEvent()
        => new("TestExecutor", new Exception("failure"));

    private static SuperStepCompletedEvent CreateSuperStepCompletedEvent()
        => new(1, new SuperStepCompletionInfo(["executor1"]));

    private static WorkflowWarningEvent CreateWorkflowWarningEvent()
        => new("warning data");
}
