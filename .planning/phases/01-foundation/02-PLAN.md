---
wave: 2
depends_on: ["01"]
files_modified:
  - src/CallCenter.Shared/Models/OrderInfo.cs
  - src/CallCenter.Shared/Models/RefundResult.cs
  - src/CallCenter.Shared/Models/CouponInfo.cs
  - src/CallCenter.Shared/Mcp/IOrderMcpClient.cs
  - src/CallCenter.Shared/Mcp/IFinanceMcpClient.cs
  - src/CallCenter.Shared/Mcp/IMemberMcpClient.cs
  - src/CallCenter.Shared/Services/MockOrderService.cs
  - src/CallCenter.Shared/Services/MockFinanceService.cs
  - src/CallCenter.Shared/Services/MockMemberService.cs
requirements: [MC-01, MC-02, MC-03, DT-01, DT-02, DT-03]
autonomous: true
---

# 计划 02：Shared 层 — 模型 + MCP 接口 + Mock 服务

## 目标

创建所有 Shared 层代码：DTO 模型、MCP 客户端接口、以及包含测试数据的 Mock 实现。

## 任务

### 任务 2.1：创建 OrderInfo 模型

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-37：C# record 定义消息类型，D-40~D-42：Mock 数据定义）
- Prd.md Section 四（目录结构参考）
</read_first>

<acceptance_criteria>
- src/CallCenter.Shared/Models/OrderInfo.cs 存在
- 定义 record：OrderInfo(string OrderId, string UserId, string ProductName, decimal Amount, DateTime OrderDate, string Status, string Category, bool HasCoupon)
- 命名空间：CallCenter.Shared.Models
</acceptance_criteria>

<action>
创建 OrderInfo.cs，C# record 包含 8 个字段。命名空间 CallCenter.Shared.Models。
</action>

### 任务 2.2：创建 RefundResult 模型

<acceptance_criteria>
- src/CallCenter.Shared/Models/RefundResult.cs 存在
- 定义 record：RefundResult(string RefundId, string OrderId, decimal Amount, string Status, DateTime RefundDate, string Message)
- 命名空间：CallCenter.Shared.Models
</acceptance_criteria>

<action>
创建 RefundResult.cs，C# record 包含 6 个字段。
</action>

### 任务 2.3：创建 CouponInfo 模型

<acceptance_criteria>
- src/CallCenter.Shared/Models/CouponInfo.cs 存在
- 定义 record：CouponInfo(string CouponId, string UserId, decimal Discount, DateTime ExpiryDate)
- 命名空间：CallCenter.Shared.Models
</acceptance_criteria>

<action>
创建 CouponInfo.cs，C# record 包含 4 个字段。
</action>

### 任务 2.4：创建 IOrderMcpClient 接口

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-26~D-29：Shared/Mcp/ 接口定义，D-28：Executor 只依赖接口）
- Prd.md Section 四（Shared/Mcp/ 目录定义）
</read_first>

<acceptance_criteria>
- src/CallCenter.Shared/Mcp/IOrderMcpClient.cs 存在
- 定义接口，包含方法：Task<OrderInfo?> GetOrderAsync(string orderId, CancellationToken ct = default)
- 定义接口，包含方法：Task<List<OrderInfo>> GetRecentOrdersAsync(string userId, CancellationToken ct = default)
- 命名空间：CallCenter.Shared.Mcp
</acceptance_criteria>

<action>
在 CallCenter.Shared.Mcp 命名空间创建 IOrderMcpClient.cs 接口，包含 2 个方法。
</action>

### 任务 2.5：创建 IFinanceMcpClient 接口

<acceptance_criteria>
- src/CallCenter.Shared/Mcp/IFinanceMcpClient.cs 存在
- 定义接口，包含方法：Task<RefundResult> RefundAsync(string orderId, decimal amount, CancellationToken ct = default)
- 命名空间：CallCenter.Shared.Mcp
</acceptance_criteria>

<action>
创建 IFinanceMcpClient.cs 接口，包含 RefundAsync 方法。
</action>

### 任务 2.6：创建 IMemberMcpClient 接口

<acceptance_criteria>
- src/CallCenter.Shared/Mcp/IMemberMcpClient.cs 存在
- 定义接口，包含方法：Task<CouponInfo?> GetCouponAsync(string userId, CancellationToken ct = default)
- 定义接口，包含方法：Task<bool> RestoreCouponAsync(string userId, string couponId, CancellationToken ct = default)
- 命名空间：CallCenter.Shared.Mcp
</acceptance_criteria>

<action>
创建 IMemberMcpClient.cs 接口，包含 2 个方法。
</action>

### 任务 2.7：创建 MockOrderService

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-40：3 个测试订单 A001/A002/A003）
</read_first>

<acceptance_criteria>
- src/CallCenter.Shared/Services/MockOrderService.cs 存在
- 实现 IOrderMcpClient 接口
- 包含 3 个硬编码订单：
  - A001：蓝牙耳机 ¥299，3 天前，Status="delivered"，Category="electronics"，HasCoupon=true
  - A002：定制T恤 ¥159，30 天前，Status="delivered"，Category="custom"
  - A003：手机壳 ¥39，1 天前，Status="shipped"，Category="electronics"
- GetOrderAsync 返回匹配的订单或 null
- GetRecentOrdersAsync 返回给定 userId 的订单（使用 "U100" 获取所有订单）
- 命名空间：CallCenter.Shared.Services
</acceptance_criteria>

<action>
创建 MockOrderService 实现 IOrderMcpClient。使用静态 Dictionary 存储 3 个订单。GetOrderAsync 按 orderId 查找。GetRecentOrdersAsync 按 userId="U100" 过滤。
</action>

### 任务 2.8：创建 MockFinanceService

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-41：RefundResult { RefundId: "RF-xxx", Status: "success" }）
</read_first>

<acceptance_criteria>
- src/CallCenter.Shared/Services/MockFinanceService.cs 存在
- 实现 IFinanceMcpClient 接口
- RefundAsync 返回 RefundResult，RefundId="RF-{Guid.NewGuid():N}"，Amount=amount，Status="success"，Message="退款已处理"
- 命名空间：CallCenter.Shared.Services
</acceptance_criteria>

<action>
创建 MockFinanceService 实现 IFinanceMcpClient。RefundAsync 生成新的 RefundId 并返回成功结果。
</action>

### 任务 2.9：创建 MockMemberService

<read_first>
- .planning/phases/01-foundation/01-CONTEXT.md（D-42：CouponInfo { CouponId: "CPN-2024", Discount: 20.00 }）
</read_first>

<acceptance_criteria>
- src/CallCenter.Shared/Services/MockMemberService.cs 存在
- 实现 IMemberMcpClient 接口
- GetCouponAsync 对任何 userId 返回 CouponInfo("CPN-2024", userId, 20.00m, DateTime.Now.AddMonths(3))
- RestoreCouponAsync 返回 true
- 命名空间：CallCenter.Shared.Services
</acceptance_criteria>

<action>
创建 MockMemberService 实现 IMemberMcpClient。两个方法都返回 Mock 数据。
</action>

### 任务 2.10：验证 Shared 层编译

<acceptance_criteria>
- `dotnet build src/CallCenter.Shared/CallCenter.Shared.csproj` 成功，0 错误
- 所有模型正确编译
</acceptance_criteria>

<action>
对 Shared 项目执行 dotnet build。修复任何编译错误。
</action>
