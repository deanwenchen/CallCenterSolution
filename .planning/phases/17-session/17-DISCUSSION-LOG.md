# Phase 17: Session 持久化存储 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-05
**Phase:** 17-Session 持久化存储
**Areas discussed:** 接口设计细节, Redis 配置策略, 序列化方案

---

## 接口设计细节

| Option | Description | Selected |
|--------|-------------|----------|
| 保持现有签名，只加 TTL | 保持当前 5 个方法签名，只给 SetAsync 加 TimeSpan? ttl 参数 | |
| 增加 TryGetAsync + 错误区分 | 把 GetAsync<T> 改成 TryGetAsync<T> 风格的返回值，区分「key 不存在」和「反序列化失败」 | ✓ |
| 加 ExistsAsync 方法 | 接口只处理 string 值的读写，序列化/反序列化交给调用方 | |

**User's choice:** 增加 TryGetAsync + 错误区分
**Notes:** 用户希望在 Redis 场景下能明确区分 key 不存在和反序列化失败两种情况。

## Redis 配置策略

| Option | Description | Selected |
|--------|-------------|----------|
| 单节点 ConnectionMultiplexer | StackExchange.Redis 单节点连接，连接字符串如 localhost:6379 | |
| Cluster 集群模式 | 支持 Redis Cluster 模式 | |
| 注入 IConnectionMultiplexer | 支持通过 IConnectionMultiplexer 接口注入已有连接 | |

**User's choice:** 以上均不适用 — 用户有自己的 Redis 封装 `Leo.Data.Redis`
**Notes:** 用户提供了自己的 Redis 链接封装，位于 `D:\babybusworkplace\LeoSolution\Leo.Data.Redis`。基于 ServiceStack.Redis 的静态 `RedisHelper` 封装，不走 DI，通过 `redisconfig.json` 配置驱动。后续确认直接复用 Leo.Data.Redis。

## 序列化方案

| Option | Description | Selected |
|--------|-------------|----------|
| 直接用 RedisHelper<T> 方法 | 调用 RedisHelper.Get<T>/Set<T>，序列化由 Leo.Data.Redis 内部处理（ServiceStack.Text） | ✓ |
| 自己序列化后存 string | CallCenter 自己做 JsonSerializer.Serialize/Deserialize，然后调 RedisHelper 的 string 方法 | |

**User's choice:** 直接用 RedisHelper<T> 方法 (推荐)
**Notes:** Leo.Data.Redis 内部使用 ServiceStack.Text 进行序列化，CallCenter 层不需要手动处理 JSON。

---

## Claude's Discretion

None — all areas were explicitly decided by the user.

## Deferred Ideas

None — discussion stayed within phase scope.
