# Phase 16: SafetyOutput + Exchange 骨架确认 - Discussion Log

**Date:** 2026-06-04
**Mode:** discuss

## Discussion Areas

### 内容审核范围
- **Question:** SafetyOutput 内容审核做到多深？
- **Options:** 关键词匹配 / LLM 语义判断 / 混合模式
- **Selection:** 关键词匹配（与输入端一致）
- **Notes:** v3.0 不需要 LLM 级审核，先跑通关键词方案

### 审核类别
- **Question:** 先拦截哪些类别的敏感内容？
- **Selection:** 暴力/恐怖、色情/低俗、政治敏感（全部三类）

### 违规话术
- **Question:** 是否按类别返回不同话术？
- **Selection:** 按类别区分话术
- **Notes:** 每类独立模板，复用 SafetyOptions 配置

### 配置方式
- **Question:** 输出端配置怎么管理？
- **Selection:** 复用 SafetyOptions 配置
- **Notes:** 不新增独立配置类

### Exchange 骨架
- **Question:** Exchange 骨架确认做到什么程度？
- **Selection:** 仅确认结构（编译通过即可）

## Decisions Summary

| # | Category | Decision |
|---|----------|----------|
| D-01 | 审核策略 | 关键词匹配，不用 LLM |
| D-02 | 审核类别 | 暴力/恐怖、色情/低俗、政治敏感 |
| D-03 | 审核时机 | LLM 响应到达用户前 |
| D-04 | 话术策略 | 按类别区分 |
| D-05 | 配置方案 | 复用 SafetyOptions |
| D-06 | 关键词来源 | appsettings.json 读取 |
| D-07 | 异常类型 | output_content_blocked |
| D-08 | 审计策略 | 不记录审计日志 |
| D-09 | Exchange | 编译通过即确认 |
| D-10 | Exchange | 业务实现属 v4 |
