using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CallCenter.Framework.Logging;

public record JsonlLogEntry(
    string Timestamp,
    string SessionId,
    string Direction,
    string? Tool,
    string Content,
    int? TokenCount);

public class JsonlLogger
{
    private readonly string _logDirectory;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public JsonlLogger(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? ".logs";
    }

    public async Task LogAsync(
        string sessionId,
        string direction,
        string content,
        string? tool = null,
        int? tokenCount = null)
    {
        await _writeLock.WaitAsync();
        try
        {
            Directory.CreateDirectory(_logDirectory);
            var filePath = Path.Combine(_logDirectory, $"{sessionId}.jsonl");
            var entry = new JsonlLogEntry(
                DateTime.UtcNow.ToString("o"),
                sessionId,
                direction,
                tool,
                content,
                tokenCount);
            var json = JsonSerializer.Serialize(entry);
            await File.AppendAllTextAsync(filePath, json + Environment.NewLine);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
