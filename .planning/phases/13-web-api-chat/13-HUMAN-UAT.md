---
status: partial
phase: 13-web-api-chat
source: [13-VERIFICATION.md]
started: 2026-06-04T11:00:00Z
updated: 2026-06-04T11:00:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Swagger UI 可访问性
expected: 设置 DASHSCOPE_API_KEY 环境变量后运行 dotnet run，浏览器访问 /swagger，Swagger UI 页面正常显示，/chat 端点文档可见
result: [pending]

### 2. POST /chat 实际返回响应
expected: curl -X POST /chat -d '{"message": "我要退款，订单A001"}' 返回 {"response": "...", "sessionId": "..."} JSON 响应
result: [pending]

### 3. 空消息返回 400
expected: curl -X POST /chat -d '{"message": ""}' 返回 400 {"error": "message is required"}
result: [pending]

### 4. CORS 跨域请求
expected: 浏览器控制台从不同 origin 发送 POST /chat 请求，不被 CORS 拦截，响应正常返回
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
