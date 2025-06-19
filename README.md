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
上传的文件夹


上传Zip游戏文件，文件只能是zip压缩，并且zip的根目录必须有游戏的启动程序exe，例如：GBA.zip

![image](https://github.com/user-attachments/assets/15397750-cfb2-4d2b-ba35-bb3507192565)






