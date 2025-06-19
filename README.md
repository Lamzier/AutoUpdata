# 自动更新程序

### 使用效果

#### 客户端

![image](https://github.com/user-attachments/assets/7d581a5d-dde9-4ee7-b8d3-8e7662931e62)
#### 服务端

![image](https://github.com/user-attachments/assets/5888be42-a43c-4a3e-ac9a-8e9d90011bd1)

### 使用环境

#### 客户端

软件：dotnet8.0 或以上

系统：Windows10、11

#### 服务端

软件: dotnet8.0 或以上

系统：Windows10、11 、 Linux

### 使用教程

#### 1、下载软件

[下载](https://github.com/Lamzier/AutoUpdata/releases/latest) 客户端 和 服务端

![image](https://github.com/user-attachments/assets/3d4fba10-2acf-4ab3-923a-1c3ab1402d12)

#### 2、部署服务端

【1】 Linux 系统

建议：先使用Screen创建一个新会话

```bash
screen -S AutoUpload
```

解压服务端

```bash
unzip Server-all.zip -d Server
cd Server
```

安装 dotnet 8.0 或以上，可以参考[微软官网](https://learn.microsoft.com/zh-cn/dotnet/core/install/linux)

这里使用脚本安装为例

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version latest
dotnet --version
```

如果出现版本大于8就可以了，例如：8.0.411

运行服务端

```bash
dotnet Server.dll
```

![fd96f56f533a3ad2257142ea87a2a77f](https://github.com/user-attachments/assets/48ab95e9-efe0-4cab-9e04-4064ee68dafd)

【2】 Windows 系统

下载并安装Dotnet8或以上的版本，可以参考[微软官网](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)

解压服务端文件，双击Server.exe 打开文件

![image](https://github.com/user-attachments/assets/b80916c9-a550-4a19-9429-c1b0faa7e3e7)

![image](https://github.com/user-attachments/assets/f56fc2ac-56ac-43b5-9a77-e25fdfb3c832)

#### 3、配置防火墙、或Dns

默认端口 **5000** ，需要配置开放端口

Dns映射域名

#### 4、上传新版本游戏

打开网址：http://{你的Ip或域名}:5000

例如：http://127.0.0.1:5000

![image](https://github.com/user-attachments/assets/c7d13369-5d17-4b55-b166-7625464e35de)

  注意事项：
  
  游戏文件：建议（非强制）上传的文件夹名称不要加上版本号，就游戏名称永恒不变
  
  名称：建议（非强制）填写游戏名称，永恒不变
  
  版本号：不要出现特殊字符
  
  启动文件：把你游戏文件根目的启动文件名称复制过来，可以使用相对路径，但是没测试过！！

上传Zip游戏文件，文件只能是zip压缩，并且zip的根目录必须有游戏的启动程序exe，例如：GBA.zip

![image](https://github.com/user-attachments/assets/15397750-cfb2-4d2b-ba35-bb3507192565)

![image](https://github.com/user-attachments/assets/e8b116eb-ea31-41f5-9a4b-09f55247b972)

出现：新版本程序提交完成 就表示新版本已经上传完毕了

#### 5、配置客户端

解压客户端文件

![image](https://github.com/user-attachments/assets/19193f99-f21c-4129-9581-a4da99b7012c)

Client-windows-all.exe 可以修改启动文件为你的游戏名称，例如：HMCL.exe

Client-windows-all.exe 图标可以通过软件改成自己的图标，推荐修改图标软件[【CN911】换exe图标](https://www.52pojie.cn/thread-1196729-1-1.html) 亲测有效

双击运行 Client-windows-all.exe，第一次会运行错误生成一个info.json文件

![image](https://github.com/user-attachments/assets/d257086a-7cef-4cbc-a544-5d50c50f829a)

修改info.json文件

```json
{
  "Id": -1,                               # 不用管，会从服务器自动更新
  "Name": "Test",                         # 不用管，会从服务器自动更新
  "ServerUrl": "http://localhost:5000",   # 修改成你的服务器地址
  "Version": "0.0.0",                     # 不用管，会从服务器自动更新
  "StartupFile": "start.exe",             # 不用管，会从服务器自动更新
  "CreateTime": "0001-01-01T00:00:00",    # 不用管，会从服务器自动更新
  "IsDeleteExcessFiles": true             # 是否启用删除更新策略
}
```






