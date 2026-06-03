#pragma warning disable MAAI001
using System.Threading.Channels;
using CallCenter.Framework;
using CallCenter.Framework.Parsing;
using CallCenter.Framework.Session;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.AgentHost;

/// <summary>
/// CallCenterService 用户交互层。
/// 主要作用：处理工作流发出的 ExternalRequest（RefundSignal 需要订单号、ConfirmRefundRequest 需要确认/取消），
/// 转成控制台上的用户交互。迁移自 Program.cs 的 HandleRequestAsync。
/// </summary>
public partial class CallCenterService
{
    /// <summary>
    /// 处理工作流对外请求（RequestPort）。
    /// 把工作流发出的"请提供订单号 / 请确认退款"这类请求，转成控制台上的用户交互。
    /// 分支 1: RefundSignal.NeedOrderId — 读取订单号，创建 RefundIntent 响应
    /// 分支 2: ConfirmRefundRequest — 显示订单信息，等待确认/取消，未识别回复时调用 _recognizeIntent
    /// 分支 3: 未知请求类型 — 抛出 NotSupportedException
    /// </summary>
    public async Task<ExternalResponse> HandleRequestAsync(ExternalRequest request, string sessionId, CancellationToken ct = default)
    {
        // Branch 1: RefundSignal (from InfoPort)
        if (request.TryGetDataAs<RefundSignal>(out var signal))
        {
            switch (signal)
            {
                case RefundSignal.NeedOrderId:
                    Console.Write("请提供订单号: ");
                    var orderId = await _inputChannel.Reader.ReadAsync(ct).ConfigureAwait(false) ?? "";
                    await _sessionStore.SetAsync("pendingOrderId", orderId, sessionId, ct).ConfigureAwait(false);
                    return request.CreateResponse(new RefundIntent(orderId, "U100"));
            }
        }

        // Branch 2: ConfirmRefundRequest (from ConfirmPort)
        if (request.TryGetDataAs<ConfirmRefundRequest>(out var confirmReq))
        {
            Console.WriteLine($"订单 {confirmReq.OrderId}: {confirmReq.ProductName} ¥{confirmReq.Amount:F2}");
            Console.Write("确认退款？(回复'确认'或'取消'): ");
            var reply = await _inputChannel.Reader.ReadAsync(ct).ConfigureAwait(false);

            if (reply == "确认")
            {
                Console.WriteLine("已确认");
                return request.CreateResponse(new UserConfirmation(true));
            }

            if (reply == "取消")
            {
                Console.WriteLine("[系统] 已取消退款");
                return request.CreateResponse(new UserConfirmation(false));
            }

            // Unrecognized reply — re-recognize intent (IR-05)
            var intent = await _recognizeIntent(reply ?? "", ct).ConfigureAwait(false);
            if (intent == null || intent.Intent == "unknown")
            {
                Console.WriteLine($"[系统] 未识别回复 '{reply}'，已取消退款");
                return request.CreateResponse(new UserConfirmation(false));
            }

            if (intent.Intent == "greeting")
            {
                Console.WriteLine("\n[系统] 你好！有什么可以帮助你的？");
                Console.WriteLine("[系统] 退款流程已挂起，请确认后重新开始");
                await _sessionStore.RemoveAsync("activeWorkflow", sessionId, ct).ConfigureAwait(false);
                return request.CreateResponse(new UserConfirmation(false));
            }

            // New workflow intent — suspend current workflow, switch intent
            Console.WriteLine("\n[系统] 已终止 RefundWorkflow 流程");
            Console.WriteLine($"[系统] 新意图 '{intent.Intent}' 暂未实现");
            await _sessionStore.RemoveAsync("activeWorkflow", sessionId, ct).ConfigureAwait(false);
            return request.CreateResponse(new UserConfirmation(false));
        }

        // Branch 3: Unknown request type
        throw new NotSupportedException($"Unknown request type: {request.PortInfo.PortId}");
    }
}
