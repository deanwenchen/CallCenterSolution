using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CallCenter.Framework.Audit;

public record AuditLogEntry(
    string Timestamp,
    string SessionId,
    string ExecutorId,
    string EventType,
    string? InputJson,
    string? OutputJson,
    string PreviousHash,
    string Hash);

public enum AuditVerificationStatus { Valid, Invalid, FileNotFound }

public record AuditVerificationResult(AuditVerificationStatus Status, int? TamperedAtLine, string? Message);

public class AuditLogger
{
    private readonly string _auditDirectory;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public AuditLogger(string? auditDirectory = null)
    {
        _auditDirectory = auditDirectory ?? ".audit";
    }

    public async Task LogAsync(
        string executorId,
        object? input,
        object? output,
        string eventType,
        string sessionId,
        CancellationToken ct = default)
    {
        var fileLock = _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(_auditDirectory);
            var filePath = GetAuditFile(sessionId);

            // Read last line to get previous hash
            var previousHash = await GetLastHashAsync(filePath, ct);

            var timestamp = DateTime.UtcNow.ToString("o");
            var entry = new AuditLogEntry(
                timestamp,
                sessionId,
                executorId,
                eventType,
                input != null ? JsonSerializer.Serialize(input) : null,
                output != null ? JsonSerializer.Serialize(output) : null,
                previousHash,
                ""); // hash computed below

            var jsonLine = JsonSerializer.Serialize(entry);
            var hash = ComputeHash(jsonLine);
            entry = entry with { Hash = hash };

            jsonLine = JsonSerializer.Serialize(entry);
            await File.AppendAllTextAsync(filePath, jsonLine + Environment.NewLine, ct);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task<AuditVerificationResult> VerifyChainAsync(string sessionId, CancellationToken ct = default)
    {
        var filePath = GetAuditFile(sessionId);
        if (!File.Exists(filePath))
            return new AuditVerificationResult(AuditVerificationStatus.FileNotFound, null, $"Audit file not found: {filePath}");

        var lines = await File.ReadAllLinesAsync(filePath, ct);
        if (lines.Length == 0)
            return new AuditVerificationResult(AuditVerificationStatus.Invalid, null, "Audit file is empty");

        string? expectedPreviousHash = null;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            AuditLogEntry entry;
            try
            {
                entry = JsonSerializer.Deserialize<AuditLogEntry>(line)!;
            }
            catch
            {
                return new AuditVerificationResult(AuditVerificationStatus.Invalid, i, $"Invalid JSON at line {i + 1}");
            }

            // Verify previousHash chain
            if (entry.PreviousHash != expectedPreviousHash)
                return new AuditVerificationResult(AuditVerificationStatus.Invalid, i,
                    $"Chain broken at line {i + 1}: expected previousHash '{expectedPreviousHash}', got '{entry.PreviousHash}'");

            // Verify entry hash
            var contentForHash = JsonSerializer.Serialize(entry with { Hash = "" });
            var actualHash = ComputeHash(contentForHash);
            if (entry.Hash != actualHash)
                return new AuditVerificationResult(AuditVerificationStatus.Invalid, i,
                    $"Hash mismatch at line {i + 1}: expected '{actualHash}', got '{entry.Hash}'");

            expectedPreviousHash = entry.Hash;
        }

        return new AuditVerificationResult(AuditVerificationStatus.Valid, null, $"Chain valid ({lines.Length} entries)");
    }

    public string GetAuditFile(string sessionId)
    {
        return Path.Combine(_auditDirectory, $"{sessionId}.jsonl");
    }

    private static async Task<string> GetLastHashAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath)) return "";

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        string? lastLine = null;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (!string.IsNullOrWhiteSpace(line)) lastLine = line;
        }

        if (lastLine == null) return "";

        try
        {
            var entry = JsonSerializer.Deserialize<AuditLogEntry>(lastLine);
            return entry?.Hash ?? "";
        }
        catch { return ""; }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
