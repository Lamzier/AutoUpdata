using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Client_windows_all;

public partial class MainWindow
{
    public static MainWindow Instance = null!;
    private DateTime _serverCreateTime;
    private int _serverId = -1;
    private string? _serverName;
    private string? _serverStartupFile;
    private string? _serverVersion;


    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        Hide();
        Init();
    }

    private async void Init()
    {
        var isUpdates = await CheckUpdates();
        if (isUpdates == false)
        {
            OpenExe(LocalInfo.GetStartupFile());
            return;
        }

        // 需要更新
        await UpdataAsync(); // 执行更新程序
    }

    private async Task UpdataAsync()
    {
        Show(); // 开始更新程序，展示窗口
        var fileList = await GetServerFileList();
        /*
        {
          "start.exe": "d41d8cd98f00b204e9800998ecf8427e",
          "排球-正面上手发球.md": "90bf63fa7217a4e5a60f421b64ab30eb",
          "篮球-行进间运球-试讲.md": "d41d8cd98f00b204e9800998ecf8427e",
          "篮球-行进间运球.md": "c864d87e0e8573f058192f38edb7b194",
          "足球-脚内侧踢球-试讲.md": "5244d4841639e8bd87d6a9705f085b8e",
          "足球-脚内侧踢球.md": "45f60afd5f9d9544891d1d1b255e8901",
          "课堂实录\\正脚面射门.docx": "d88ccaf874f9d879d9681df3bf37bbe6",
          "课堂实录\\正脚面射门.md": "d01f86cae432ebc5c01f3124a0a03cc8",
          "课堂实录\\正脚面运球.docx": "010636d813199720ea4cd15aa769faee",
          "课堂实录\\正脚面运球.md": "6e500123606de5c1ce2a365d5b3b215a",
          "课堂实录\\脚内侧接球.docx": "c2529daa1bb5d90aa334d838f61e9f6c",
          "课堂实录\\脚内侧接球.md": "915c437294163793a144719948061d72",
          "课堂实录\\脚内侧运球.docx": "85cbe51212b185484d34c5fec7be44c6",
          "课堂实录\\脚内侧运球.md": "ac45c49895b06edaa0f6fbd1b1b3c0f3",
          "课堂实录\\课堂教学实录.xlsx": "a1ebd8da5c153ce975f456553c53ab08",
          "课堂实录\\asdasdasd\\ddsad\\asd.txt": "d41d8cd98f00b204e9800998ecf8427e"
        }
        */
        await DeleteUnnecessaryFiles(fileList); // 删除多余文件
        await UpdateFiles(fileList); // 对比Md5选择更新文件
    }

    /**
     * 更新文件
     */
    private async Task UpdateFiles(dynamic fileList)
    {
        // 对比md5选择更新文件
        // 文件下载地址：LocalInfo.GetServerUrl() + $"/static/upload/{_serverName}-{_serverVersion}/***"
        // 例如下载start.exe则 LocalInfo.GetServerUrl() + $"/static/upload/{_serverName}-{_serverVersion}/start.exe"
        // 将动态类型转换为 JObject
        if (fileList is not JObject jObject)
        {
            AddStatusText("× 无效的文件列表格式");
            return;
        }

        const int bufferSize = 81920;
        var serverBaseUrl = $"{LocalInfo.GetServerUrl()}/static/upload/{_serverName}-{_serverVersion}/";
        var localRoot = Path.Combine(Directory.GetCurrentDirectory(), _serverName!);
        try
        {
            AddStatusText("开始文件更新流程");

            // 正确获取文件总数
            var totalFiles = jObject.Properties().Count();
            UpdateProgress.Maximum = totalFiles;
            UpdateProgress.Value = 0;
            foreach (var prop in jObject.Properties())
            {
                UpdateProgress.Value++;
                try
                {
                    var relativePath = prop.Name.Replace('/', '\\');
                    var serverMd5 = prop.Value.ToString().ToLower();
                    var localPath = Path.Combine(localRoot, relativePath);
                    var downloadUrl = new Uri($"{serverBaseUrl}{Uri.EscapeDataString(prop.Name)}");

                    ProgressText.Text = $"{relativePath} ({UpdateProgress.Value}/{totalFiles})";
                    if (NeedUpdate(localPath, serverMd5))
                    {
                        AddStatusText($"更新文件: {relativePath}");
                        await DownloadFileWithRetry(downloadUrl, localPath, bufferSize);
                    }

                    else
                    {
                        AddStatusText($"无需更新: {relativePath}");
                    }
                }
                catch (Exception ex)
                {
                    AddStatusText($"文件处理失败: {ex.Message}");
                }
            }

            AddStatusText($"更新完成 ({totalFiles} 个文件已处理)");
            UpdateProgress.Value = 0;
        }
        catch (Exception ex)
        {
            AddStatusText($"更新过程中断: {ex.Message}");
        }
    }

    private async Task DownloadFileWithRetry(Uri url, string savePath, int bufferSize, int retries = 3)
    {
        for (var i = 1; i <= retries; i++)
            try
            {
                // 创建目录结构
                var dir = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                using var client = new HttpClient();
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                // 获取文件长度用于进度计算
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                using var sourceStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);

                var buffer = new byte[bufferSize];
                long bytesRead = 0;
                int read;

                while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    bytesRead += read;

                    // 更新下载进度
                    if (totalBytes > 0)
                    {
                        var progress = (double)bytesRead / totalBytes * 100;
                        Dispatcher.Invoke(() => UpdateProgress.Value = progress);
                    }
                }

                return; // 下载成功
            }
            catch (Exception ex) when (i < retries)
            {
                AddStatusText($"× 下载失败 ({i}/{retries}): {ex.Message}");
                await Task.Delay(3000 * i);
            }

        throw new Exception($"文件下载失败: {Path.GetFileName(savePath)}");
    }

    // 检查是否需要从网络更新
    private bool NeedUpdate(string localPath, string serverMd5)
    {
        // 文件不存在需要下载
        if (!File.Exists(localPath)) return true;

        // 计算本地MD5
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(localPath);
        var localHash = BitConverter.ToString(md5.ComputeHash(stream))
            .Replace("-", "").ToLower();

        return !localHash.Equals(serverMd5);
    }

    /**
     * 删除多余的文件和目录
     */
    private async Task DeleteUnnecessaryFiles(dynamic fileList)
    {
        try
        {
            // 生成服务器路径集合（包含文件+目录）
            var serverPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var serverDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in fileList)
            {
                string path = prop.Name.Replace('\\', '/');

                // 添加文件路径
                serverPaths.Add(path);

                // 提取所有父目录路径
                var dirPath = path;
                while (true)
                {
                    var lastSlash = dirPath.LastIndexOf('/');
                    if (lastSlash <= 0) break;

                    dirPath = dirPath.Substring(0, lastSlash);
                    serverDirectories.Add(dirPath + "/"); // 目录标识
                }
            }

            // 合并有效路径（文件+必须保留的目录）
            var validPaths = new HashSet<string>(serverPaths
                    .Concat(serverDirectories),
                StringComparer.OrdinalIgnoreCase
            );

            var localRoot = Path.Combine(Directory.GetCurrentDirectory(), _serverName!);
            AddStatusText($"开始深度清理，根目录: {localRoot}");

            // 获取本地所有路径（文件+目录）
            var allLocalPaths = new List<string>();

            // 文件路径
            foreach (var file in Directory.EnumerateFiles(localRoot, "*", SearchOption.AllDirectories))
            {
                var relPath = GetRelativePath(file, localRoot);
                allLocalPaths.Add(relPath);
            }

            // 目录路径（包含空目录）
            foreach (var dir in Directory.EnumerateDirectories(localRoot, "*", SearchOption.AllDirectories))
            {
                var relPath = GetRelativePath(dir, localRoot) + "/"; // 目录标识
                allLocalPaths.Add(relPath);
            }

            // 需要删除的路径
            var pathsToDelete = allLocalPaths
                .Where(p => !validPaths.Contains(p.Replace('\\', '/')))
                .OrderByDescending(p => p.Length)
                .ToList();

            // 删除文件
            foreach (var relPath in pathsToDelete.Where(p => !p.EndsWith("/")))
            {
                var fullPath = Path.Combine(localRoot, relPath);
                if (!File.Exists(fullPath)) continue;
                try
                {
                    File.Delete(fullPath);
                    AddStatusText($"删除文件: {relPath}");
                }
                catch (Exception ex)
                {
                    AddStatusText($"文件删除失败 [{relPath}]: {ex.Message}");
                }
            }

            // 删除目录（深度优先）
            foreach (var relPath in pathsToDelete.Where(p => p.EndsWith("/")))
            {
                var dirPath = Path.Combine(localRoot, relPath.TrimEnd('/'));
                if (!Directory.Exists(dirPath)) continue;
                try
                {
                    // 尝试删除空目录
                    Directory.Delete(dirPath, false);
                    AddStatusText($"删除空目录: {relPath}");
                }
                catch (IOException)
                {
                    // 非空目录强制删除
                    try
                    {
                        Directory.Delete(dirPath, true);
                        AddStatusText($"强制删除目录: {relPath}");
                    }
                    catch (Exception ex)
                    {
                        AddStatusText($"目录删除失败 [{relPath}]: {ex.Message}");
                    }
                }
            }

            AddStatusText("深度清理完成");
        }
        catch (Exception ex)
        {
            AddStatusText($"清理异常: {ex.Message}");
        }
    }

    /**
     * 获取相对路径（统一用斜杠）
     */
    private string GetRelativePath(string fullPath, string basePath)
    {
        var uri = new Uri(fullPath);
        var baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
        var relative = Uri.UnescapeDataString(baseUri.MakeRelativeUri(uri).ToString());
        return relative.Replace(Path.DirectorySeparatorChar, '/'); // 统一为斜杠
    }

    /**
     * 获取服务器文件列表
     */
    private async Task<dynamic?> GetServerFileList()
    {
        try
        {
            // 构建请求URL
            var serverFileListUrl = LocalInfo.GetServerUrl() + $"/static/upload/{_serverName}-{_serverVersion}.json";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ClientApp/1.0");
            var response = await client.GetAsync(serverFileListUrl);
            if (!response.IsSuccessStatusCode)
            {
                AddStatusText($"获取服务器文件列表请求失败：{response.StatusCode}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonResponse) ?? throw new InvalidOperationException();
            if (data == null) Instance.AddStatusText("获取服务器文件列表请求失败!");
            return data;
        }
        catch (Exception ex)
        {
            Instance.AddStatusText($"获取服务器文件列表失败：{ex.Message}");
            return null;
        }
    }

    private void OpenExe(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                AddStatusText("错误：未配置主程序路径");
                MessageBox.Show("应用程序配置错误，请重新安装程序!", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                AddStatusText($"错误：找不到主程序文件 [{fullPath}]");
                MessageBox.Show($"主程序文件不存在：\n{fullPath}", "文件丢失", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 启动进程
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true, // 允许通过Shell执行
                    WorkingDirectory = Path.GetDirectoryName(fullPath) // 设置工作目录
                }
            };
            if (process.Start())
            {
                AddStatusText($"已启动主程序 [{Path.GetFileName(fullPath)}]");
                Task.Delay(800).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(); });
                });
            }
            else
            {
                AddStatusText("错误：未知原因导致启动失败");
                MessageBox.Show("无法启动主程序，请手动运行", "启动错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Win32Exception ex)
        {
            var errorType = ex.NativeErrorCode switch
            {
                0x00000002 => "文件未找到",
                0x00000005 => "权限不足",
                0x000000D1 => "需要管理员权限",
                _ => "系统错误"
            };
            AddStatusText($"系统错误 ({errorType}): {ex.Message}");
            MessageBox.Show($"{errorType}\n{ex.Message}", "系统限制", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            AddStatusText($"意外错误: {ex.GetType().Name} - {ex.Message}");
            MessageBox.Show($"启动失败: {ex.Message}", "未知错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // 关闭当前程序
            Console.WriteLine("关闭所有程序");
        }
    }

    /**
     * 检查更新 ,返回Flase则不用更新，True则需要更新
     */
    private async Task<bool> CheckUpdates()
    {
        var server = LocalInfo.GetServerUrl();
        var version = LocalInfo.GetVersion();
        AddStatusText("正在连接服务器......");
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(60);
        // 发送版本请求
        AddStatusText($"正在获取服务器版本 ({server}/api/version)");
        var response = await httpClient.GetAsync($"{server}/api/version");
        // 处理 HTTP 错误
        if (response.StatusCode != HttpStatusCode.OK)
        {
            AddStatusText($"服务器返回错误状态码: {(int)response.StatusCode}");
            AddStatusText("检查更新失败！请重启软件重试！");
            Show(); // 展示窗口
            return false;
        }

        // 解析 JSON
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            dynamic json = JsonConvert.DeserializeObject(content)!;
            if (json.code != 0 || json.data?.version == null)
            {
                AddStatusText("无效的服务器响应格式!");
                AddStatusText("检查更新失败！请重试！");
                return false;
            }

            string serverVersion = json.data.version;
            AddStatusText($"服务器版本：{serverVersion}");
            if (version.Equals(serverVersion))
            {
                AddStatusText("已经是最新版本！无需更新！");
                return false;
            }

            AddStatusText("有新版本！准备更新...");
            // 写入新版本信息
            _serverId = json.data.id;
            _serverName = json.data.name;
            _serverVersion = json.data.version;
            _serverStartupFile = json.data.startupFile;
            _serverCreateTime = json.data.createTime;
            return true;
        }
        catch (Exception ex)
        {
            AddStatusText($"JSON解析失败: {ex.Message}");
            AddStatusText("检查更新失败！请重试！");
            return false;
        }
    }

    private void StatusText_TextChanged(object sender, SizeChangedEventArgs e)
    {
        StatusScrollViewer.ScrollToEnd();
    }

    public void AddStatusText(string text)
    {
        Console.WriteLine(text);
        StatusText.Text += text + Environment.NewLine;
    }
}