<div align="center">
  <h1>🚀 自动更新程序</h1>
  <p>精准、高效的游戏更新解决方案 - 支持Windows和Linux环境</p>
</div>

## 🌟 软件特点

| 特性 | 描述 | 优势 |
|------|------|------|
| **精准更新** | 通过MD5比对文件差异 | 仅下载更新所需文件，节省带宽和时间 |
| **双更新策略** | 删除多余文件/保留文件策略 | 适应不同游戏数据存储需求 |
| **零代码侵入** | 无需修改游戏代码 | 即插即用，随时可移除 |
| **跨平台支持** | 服务端支持Windows/Linux | 灵活选择部署环境 |

## 🖥️ 使用效果

### 客户端界面
![客户端界面](https://github.com/user-attachments/assets/7d581a5d-dde9-4ee7-b8d3-8e7662931e62)

### 服务端界面
![服务端界面](https://github.com/user-attachments/assets/5888be42-a43c-4a3e-ac9a-8e9d90011bd1)

---

## 📋 系统要求

### 客户端
| 项目 | 要求 |
|------|------|
| **操作系统** | Windows 10/11 |
| **运行环境** | .NET 8.0+ |

### 服务端
| 项目 | 要求 |
|------|------|
| **操作系统** | Windows 10/11, Linux |
| **运行环境** | .NET 8.0+ |

---

## 📚 使用教程

### 1️⃣ 下载软件
[![下载按钮](https://img.shields.io/badge/下载最新版本-00BFFF?style=for-the-badge&logo=github)](https://github.com/Lamzier/AutoUpdata/releases/latest)

![下载页面](https://github.com/user-attachments/assets/3d4fba10-2acf-4ab3-923a-1c3ab1402d12)

### 2️⃣ 部署服务端

#### Linux系统部署
```bash
# 创建新会话
screen -S AutoUpload

# 解压服务端
unzip Server-all.zip -d Server
cd Server

# 安装.NET 8.0+
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version latest
dotnet --version

# 运行服务端
dotnet Server.dll
```

![Linux运行效果](https://github.com/user-attachments/assets/48ab95e9-efe0-4cab-9e04-4064ee68dafd)

#### Windows系统部署
1. [下载安装.NET 8.0+](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)
2. 解压服务端文件
3. 双击运行`Server.exe`

![Windows解压](https://github.com/user-attachments/assets/b80916c9-a550-4a19-9429-c1b0faa7e3e7)
![Windows运行](https://github.com/user-attachments/assets/f56fc2ac-56ac-43b5-9a77-e25fdfb3c832)

### 3️⃣ 网络配置
- 开放默认端口：**5000**
- 配置防火墙规则
- 设置DNS映射（可选）

### 4️⃣ 上传游戏版本
访问管理界面：`http://{服务器IP}:5000`

![管理界面](https://github.com/user-attachments/assets/c7d13369-5d17-4b55-b166-7625464e35de)

> **📌 重要提示**
> - 游戏文件夹名称建议使用永恒不变的名称
> - 版本号不要包含特殊字符
> - 启动文件路径使用相对路径

**上传步骤：** 
1. 填写游戏名称（永恒不变）
2. 输入版本号（无特殊字符）
3. 指定启动文件名称
4. 上传ZIP格式的游戏文件

![上传ZIP文件](https://github.com/user-attachments/assets/15397750-cfb2-4d2b-ba35-bb3507192565)
![上传成功提示](https://github.com/user-attachments/assets/e8b116eb-ea31-41f5-9a4b-09f55247b972)

### 5️⃣ 配置客户端
1. 解压客户端文件
2. 重命名`Client-windows-all.exe`为游戏名称
3. 修改EXE图标（推荐使用[图标修改工具](https://www.52pojie.cn/thread-1196729-1-1.html)）

![客户端文件](https://github.com/user-attachments/assets/19193f99-f21c-4129-9581-a4da99b7012c)

**首次运行**会生成`info.json`文件：
```json
{
  "Id": -1,                               // 自动更新（无需修改）
  "Name": "Test",                         // 自动更新（无需修改）
  "ServerUrl": "http://localhost:5000",   // 修改为你的服务器地址
  "Version": "0.0.0",                     // 自动更新（无需修改）
  "StartupFile": "start.exe",             // 自动更新（无需修改）
  "CreateTime": "0001-01-01T00:00:00",    // 自动更新（无需修改）
  "IsDeleteExcessFiles": true             // 更新策略：true=删除多余文件，false=保留
}
```

![首次运行](https://github.com/user-attachments/assets/d257086a-7cef-4cbc-a544-5d50c50f829a)

### 6️⃣ 打包分发
配置完成后，打包客户端文件即可分发给用户。建议：
1. 运行客户端完成首次更新
2. 确认游戏正常运行
3. 将更新后的游戏目录打包分发

---

## 🔧 更新策略说明

| 策略 | `IsDeleteExcessFiles` | 适用场景 |
|------|------------------------|----------|
| **删除多余文件** | `true` | 用户数据存储在游戏目录外（如文档目录） |
| **保留所有文件** | `false` | 用户数据直接存储在游戏目录内 |

> **💡 策略选择建议**  
> 如果你的游戏将存档、设置等用户数据保存在`我的文档`等独立位置，选择`true`（删除策略）  
> 如果用户数据保存在游戏目录内，选择`false`（保留策略）避免误删用户数据

---

## ❓ 常见问题

<details>
<summary><strong>Q: 如何修改客户端启动文件名称？</strong></summary>
A: 直接重命名`Client-windows-all.exe`即可，名称不影响程序功能
</details>

<details>
<summary><strong>Q: 服务端支持哪些Linux发行版？</strong></summary>
A: 支持所有能运行.NET 8.0+的Linux发行版（Ubuntu, CentOS, Debian等）
</details>

<details>
<summary><strong>Q: 是否支持HTTPs？</strong></summary>
A: 当前版本仅支持HTTP，后续版本将添加HTTPS支持
</details>

<details>
<summary><strong>Q: 最大支持多大的游戏包？</strong></summary>
A: 理论无限制，已测试1GB游戏包更新正常
</details>

<details>
<summary><strong>Q: 更新过程中断怎么办？</strong></summary>
A: 程序支持断点续传，重新启动客户端会自动继续更新
</details>

---

## 📜 许可证

**MIT License**  

Copyright (c) [年份] [作者名称]  

特此免费授予任何获得本软件副本和相关文档文件（以下简称"软件"）的人不受限制地处理本软件的权限，包括但不限于使用、复制、修改、合并、发布、分发、再许可和/或销售本软件副本的权利，并允许向其提供本软件的人这样做，但须符合以下条件：  

上述版权声明和本许可声明应包含在本软件的所有副本或重要部分中。  

本软件按"原样"提供，不提供任何明示或暗示的保证，包括但不限于适销性保证、特定用途适用性保证和非侵权保证。在任何情况下，作者或版权持有人均不对因本软件或本软件的使用或其他交易而产生、引起或与之相关的任何索赔、损害赔偿或其他责任承担责任，无论是在合同诉讼、侵权行为还是其他方面。  

---

## ✨ 贡献指南

欢迎贡献代码和提交问题报告！请遵循以下步骤：

1. Fork 项目仓库  
2. 创建新分支 (`git checkout -b feature/your-feature`)  
3. 提交更改 (`git commit -am 'Add some feature'`)  
4. 推送到分支 (`git push origin feature/your-feature`)  
5. 创建新的 Pull Request  

---

## 📬 联系我们

如有任何问题或建议，请联系我们：  

- 邮箱: support@example.com  
- GitHub Issues: [https://github.com/Lamzier/AutoUpdata/issues](https://github.com/Lamzier/AutoUpdata/issues)  

---

<div align="center">
  <p>感谢使用自动更新程序！</p>
  <p>❤️ 如果本软件对您有帮助，请考虑给项目点个 Star ⭐️</p>
  <a href="https://github.com/Lamzier/AutoUpdata">
    <img src="https://img.shields.io/github/stars/Lamzier/AutoUpdata?style=social" alt="GitHub Stars">
  </a>
</div>
