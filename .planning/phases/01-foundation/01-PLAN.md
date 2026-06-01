---
wave: 1
depends_on: []
files_modified:
  - CallCenterSolution.slnx
  - Directory.Packages.props
  - src/CallCenter.Shared/CallCenter.Shared.csproj
  - src/CallCenter.Framework/CallCenter.Framework.csproj
  - src/CallCenter.Workflows/CallCenter.Workflows.csproj
  - src/CallCenter.AgentHost/CallCenter.AgentHost.csproj
  - src/CallCenter.ConsoleDemo/CallCenter.ConsoleDemo.csproj
requirements: []
autonomous: true
---

# 计划 01：项目结构 + 解决方案 + 包管理

## 目标

创建所有项目文件：解决方案、集中包管理、5 个 .csproj 文件及其正确的依赖关系。

## 任务

### 任务 1.1：创建 Directory.Packages.props

<read_first>
- ../../../GitCode/agent-framework/dotnet/Directory.Packages.props（CPM 格式参考）
</read_first>

<acceptance_criteria>
- Directory.Packages.props 存在于仓库根目录
- 包含 ManagePackageVersionsCentrally=true
- 包含所有必需的包版本：OpenAI 2.10.0、Microsoft.Extensions.AI 10.5.1、Microsoft.Extensions.AI.OpenAI 10.5.1、Microsoft.Extensions.DependencyInjection 10.0.1、Microsoft.Extensions.Configuration 10.0.1、Microsoft.Extensions.Configuration.EnvironmentVariables 10.0.1、Microsoft.Extensions.Configuration.Json 10.0.1、System.Text.Json 10.0.6
</acceptance_criteria>

<action>
在仓库根目录 (D:/Claude/CallCenterSolution1/) 创建 Directory.Packages.props，使用集中包管理。参考 MAF 文件格式。包含上述 8 个包版本。
</action>

### 任务 1.2：更新 CallCenterSolution.slnx

<read_first>
- CallCenterSolution.slnx（现有文件，包含 4 个 MAF 源码项目引用）
</read_first>

<acceptance_criteria>
- CallCenterSolution.slnx 包含现有的 4 个 MAF 源码项目引用
- CallCenterSolution.slnx 包含 5 个新的 CallCenter 项目引用：src/CallCenter.Shared/CallCenter.Shared.csproj、src/CallCenter.Framework/CallCenter.Framework.csproj、src/CallCenter.Workflows/CallCenter.Workflows.csproj、src/CallCenter.AgentHost/CallCenter.AgentHost.csproj、src/CallCenter.ConsoleDemo/CallCenter.ConsoleDemo.csproj
- 解决方案中共 9 个项目
</acceptance_criteria>

<action>
编辑 CallCenterSolution.slnx，添加 5 个新的 CallCenter 项目 Project 条目。保持现有的 4 个 MAF 源码项目引用不变。
</action>

### 任务 1.3：创建 src 目录结构

<acceptance_criteria>
- src/CallCenter.Shared/ 目录存在
- src/CallCenter.Framework/ 目录存在
- src/CallCenter.Workflows/ 目录存在
- src/CallCenter.AgentHost/ 目录存在
- src/CallCenter.ConsoleDemo/ 目录存在
- 子目录：src/CallCenter.Shared/Models/、src/CallCenter.Shared/Services/、src/CallCenter.Shared/Mcp/、src/CallCenter.Framework/EventBus/、src/CallCenter.Framework/Parsing/、src/CallCenter.Framework/Builder/、src/CallCenter.Framework/Session/、src/CallCenter.Framework/Safety/、src/CallCenter.Framework/Compaction/、src/CallCenter.Framework/Audit/、src/CallCenter.Framework/Saga/、src/CallCenter.Framework/Pipeline/、src/CallCenter.Workflows/Refund/、src/CallCenter.Workflows/Refund/Executors/、src/CallCenter.Workflows/Shared/、src/CallCenter.AgentHost/Skills/
</acceptance_criteria>

<action>
使用 mkdir -p 创建所有目录。
</action>

### 任务 1.4：创建 CallCenter.Shared.csproj

<acceptance_criteria>
- src/CallCenter.Shared/CallCenter.Shared.csproj 存在
- TargetFramework=net10.0，Nullable=enable，ImplicitUsings=enable
- PackageReference: System.Text.Json
- 无项目引用
</acceptance_criteria>

<action>
创建 CallCenter.Shared.csproj，参照 MAF 样例格式。OutputType 默认为 Library。TargetFramework net10.0。Nullable enable。ImplicitUsings enable。从 CPM 引用 System.Text.Json。
</action>

### 任务 1.5：创建 CallCenter.Framework.csproj

<acceptance_criteria>
- src/CallCenter.Framework/CallCenter.Framework.csproj 存在
- TargetFramework=net10.0，Nullable=enable，ImplicitUsings=enable
- ProjectReference 到 ../CallCenter.Shared/CallCenter.Shared.csproj
- PackageReferences: Microsoft.Extensions.DependencyInjection、Microsoft.Extensions.Configuration、System.Text.Json
</acceptance_criteria>

<action>
创建 CallCenter.Framework.csproj，ProjectReference 到 Shared。从 CPM 引用包。
</action>

### 任务 1.6：创建 CallCenter.Workflows.csproj

<acceptance_criteria>
- src/CallCenter.Workflows/CallCenter.Workflows.csproj 存在
- TargetFramework=net10.0，Nullable=enable，ImplicitUsings=enable
- ProjectReference 到 ../CallCenter.Shared/CallCenter.Shared.csproj
- ProjectReference 到 MAF Workflows：../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/Microsoft.Agents.AI.Workflows.csproj
</acceptance_criteria>

<action>
创建 CallCenter.Workflows.csproj，包含两个项目引用：Shared 和 MAF Workflows。使用匹配现有 .slnx 模式的相对路径。
</action>

### 任务 1.7：创建 CallCenter.AgentHost.csproj

<acceptance_criteria>
- src/CallCenter.AgentHost/CallCenter.AgentHost.csproj 存在
- TargetFramework=net10.0，Nullable=enable，ImplicitUsings=enable
- ProjectReference 到 ../CallCenter.Workflows/CallCenter.Workflows.csproj
- ProjectReference 到 ../CallCenter.Framework/CallCenter.Framework.csproj
- ProjectReference 到 MAF AI：../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI/Microsoft.Agents.AI.csproj
- PackageReferences: OpenAI、Microsoft.Extensions.AI.OpenAI、Microsoft.Extensions.DependencyInjection
</acceptance_criteria>

<action>
创建 CallCenter.AgentHost.csproj，包含 3 个项目引用（Workflows、Framework、MAF.AI）和 3 个来自 CPM 的包引用。
</action>

### 任务 1.8：创建 CallCenter.ConsoleDemo.csproj

<acceptance_criteria>
- src/CallCenter.ConsoleDemo/CallCenter.ConsoleDemo.csproj 存在
- OutputType=Exe，TargetFramework=net10.0，Nullable=enable，ImplicitUsings=enable
- ProjectReference 到 ../CallCenter.AgentHost/CallCenter.AgentHost.csproj
- ProjectReference 到 ../CallCenter.Workflows/CallCenter.Workflows.csproj
- ProjectReference 到 ../CallCenter.Shared/CallCenter.Shared.csproj
- ProjectReference 到 ../CallCenter.Framework/CallCenter.Framework.csproj
- ProjectReference 到 MAF Workflows：../../GitCode/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/Microsoft.Agents.AI.Workflows.csproj
- PackageReferences: Microsoft.Extensions.Configuration.EnvironmentVariables、Microsoft.Extensions.Configuration.Json
</acceptance_criteria>

<action>
创建 CallCenter.ConsoleDemo.csproj，OutputType=Exe。4 个本地项目引用 + 1 个 MAF Workflows。2 个来自 CPM 的包引用。
</action>

### 任务 1.9：验证构建

<acceptance_criteria>
- `dotnet build` 成功，0 错误
- 所有 5 个项目编译成功
</acceptance_criteria>

<action>
从仓库根目录执行 `dotnet build`。修复任何编译错误。项目应在空源文件情况下编译成功。
</action>
