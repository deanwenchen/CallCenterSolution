#pragma warning disable MAAI001
using System;
using CallCenter.AgentHost;

using var svc = new CallCenterService();
var sessionId = "demo-session";

Console.WriteLine("=== CallCenter AI Demo ===");
Console.WriteLine("输入消息开始（如'我要退款，订单A001'），输入'quit'退出。\n");

while (true)
{
    Console.Write("用户: ");
    var userMessage = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
    var result = await svc.ProcessAsync(sessionId, userMessage);
    Console.WriteLine($"系统: {result}");
}
