using CallCenter.Framework.Compaction;
using CallCenter.Framework.Logging;
using CallCenter.Framework.Safety;
using CallCenter.Framework.ToolApproval;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CallCenter.Framework.Pipeline;

/// <summary>
/// 标准聊天管道工厂。
/// 主要作用：把安全、日志、压缩、工具审批等横切能力统一组装到一次 LLM 调用链中。
/// 创建 6 层委托链：
/// SafetyInput(最外层) → Logging → Compaction → ToolApproval → LLM(最内层) → SafetyOutput
/// 外层先执行，内层后执行，形成洋葱模型结构。
/// </summary>
public static class StandardPipelineFactory
{
    /// <summary>
    /// Creates a fully-wired 6-layer pipeline: SafetyInput → Logging → Compaction → ToolApproval → LLM → SafetyOutput
    /// </summary>
    public static IChatClient CreatePipeline(
        IChatClient baseClient,
        IChatClient summarizerClient,
        string sessionId,
        JsonlLogger? logger = null,
        Safety.KeywordFilter? keywordFilter = null,
        SafetyOptions? safetyOptions = null)
    {
        logger ??= new JsonlLogger();
        safetyOptions ??= new SafetyOptions();

        // Layer 1 (innermost): baseClient — raw LLM
        IChatClient client = baseClient;

        // Layer 2: SafetyOutput — redact PII from LLM responses + content filtering
        var contentFilter = new OutputContentFilter(safetyOptions);
        client = new SafetyOutputDelegatingClient(client, safetyOptions, contentFilter);

        // Layer 3: ToolApproval — check tool calls before execution
        client = new ToolApprovalDelegatingClient(client, new DefaultToolApprovalAgent(), sessionId);

        // Layer 4: Compaction — via ChatClientBuilder with CompactionProvider
        client = CreateCompactedClient(baseClient, summarizerClient);

        // Layer 5: Logging — log requests and responses
        client = new LoggingDelegatingClient(client, logger, sessionId);

        // Layer 6 (outermost): SafetyInput — redact PII, block keywords, detect injection
        client = new SafetyInputDelegatingClient(client, keywordFilter);

        return client;
    }

    private static IChatClient CreateCompactedClient(IChatClient baseClient, IChatClient summarizerClient)
    {
        return baseClient
            .AsBuilder()
            .UseCallCenterCompaction(summarizerClient)
            .Build();
    }

    /// <summary>
    /// Creates a summarizer client using a smaller/cheaper model.
    /// </summary>
    public static IChatClient CreateSummarizerClient(
        OpenAIClient openAIClient,
        string model = "qwen-plus")
    {
        return openAIClient.GetChatClient(model).AsIChatClient();
    }
}

/// <summary>安全输出包装器。对 AI 响应应用 PII 脱敏和可选的输出端内容审核，防止敏感信息泄露给用户。</summary>
internal sealed class SafetyOutputDelegatingClient : DelegatingChatClient
{
    private readonly SafetyOptions _options;
    private readonly OutputContentFilter? _contentFilter;

    public SafetyOutputDelegatingClient(IChatClient inner, SafetyOptions options, OutputContentFilter? contentFilter = null) : base(inner)
    {
        _options = options;
        _contentFilter = contentFilter;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);
        if (response.Text is not null)
        {
            try
            {
                var safeOutput = SafetyOutputFilter.ProcessOutput(response.Text, _options, _contentFilter);
                return new ChatResponse([new ChatMessage(ChatRole.Assistant, safeOutput)])
                {
                    FinishReason = response.FinishReason,
                    Usage = response.Usage,
                };
            }
            catch (SafetyViolationException ex)
            {
                return new ChatResponse([new ChatMessage(ChatRole.Assistant, ex.Message)])
                {
                    FinishReason = response.FinishReason,
                    Usage = response.Usage,
                };
            }
        }
        return response;
    }
}

/// <summary>日志包装器。记录每次 LLM 请求和响应的内容到 JsonlLogger。</summary>
internal sealed class LoggingDelegatingClient : DelegatingChatClient
{
    private readonly JsonlLogger _logger;
    private readonly string _sessionId;

    public LoggingDelegatingClient(IChatClient inner, JsonlLogger logger, string sessionId) : base(inner)
    {
        _logger = logger;
        _sessionId = sessionId;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Log request (first user message text)
        var userText = messages.FirstOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";
        await _logger.LogAsync(_sessionId, "request", userText, tokenCount: null);

        var response = await base.GetResponseAsync(messages, options, cancellationToken);

        // Log response
        await _logger.LogAsync(_sessionId, "response", response.Text ?? "", tokenCount: response.Usage?.TotalTokenCount != null ? (int)response.Usage.TotalTokenCount : null);

        return response;
    }
}

/// <summary>安全输入包装器。对用户消息应用安全过滤（PII 脱敏 + 关键词 + 注入检测）。</summary>
internal sealed class SafetyInputDelegatingClient : DelegatingChatClient
{
    private readonly Safety.KeywordFilter? _keywordFilter;

    public SafetyInputDelegatingClient(IChatClient inner, Safety.KeywordFilter? keywordFilter = null) : base(inner)
    {
        _keywordFilter = keywordFilter;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var safeMessages = new List<ChatMessage>();
        foreach (var msg in messages)
        {
            if (msg.Role == ChatRole.User && msg.Text is not null)
            {
                var safeText = SafetyInputFilter.ProcessInput(msg.Text, "pipeline", _keywordFilter);
                safeMessages.Add(new ChatMessage(ChatRole.User, safeText));
            }
            else
            {
                safeMessages.Add(msg);
            }
        }
        return await base.GetResponseAsync(safeMessages, options, cancellationToken);
    }
}
