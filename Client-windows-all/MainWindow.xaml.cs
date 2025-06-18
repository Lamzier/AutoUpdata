using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Client_windows_all;

public partial class MainWindow
{
    public static MainWindow Instance = null!;

    // 优化AddStatusText方法
    private readonly StringBuilder _logBuffer = new();
    private DateTime _lastLogFlush = DateTime.MinValue;

    private DateTime _lastProgressUpdate = DateTime.MinValue;
    private DateTime _serverCreateTime;
    private int _serverId = -1;
    private string _serverName = null!;
    private string _serverStartupFile = null!;
    private string _serverVersion = null!;


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
            // 组合服务器目录和启动文件路径
            var fullStartupPath = Path.Combine(
                Directory.GetCurrentDirectory(), // 当前程序目录
                LocalInfo.Name, // 服务器名称目录
                LocalInfo.StartupFile // 启动文件名
            );
            OpenExe(fullStartupPath);
            return;
        }

        // 需要更新
        _ = UpdataAsync(); // 执行更新程序
    }

    private async Task UpdataAsync()
    {
        Show(); // 开始更新程序，展示窗口
        UpdateProgress.Value = 0;
        var fileList = await GetServerFileList();
        UpdateProgress.Value = 10;
        if (LocalInfo.IsDeleteExcessFiles) // 开启了删除文件
            await DeleteUnnecessaryFiles(fileList); // 删除多余文件
        UpdateProgress.Value = 20;
        await UpdateFiles(fileList); // 对比Md5选择更新文件
        UpdateProgress.Value = 100;
        UploadLocalInfo(); // 更新版本信息到本地文件
        // await SaveLogger(); // 保存日志到本地
        ReStartApp(); // 重启当前程序
    }

    private void ReStartApp()
    {
        try
        {
            // 获取当前程序路径和VBS脚本路径
            var currentDir = Directory.GetCurrentDirectory();
            var vbsPath = Path.Combine(currentDir, "restart.vbs");
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;

            // 验证关键文件存在性
            if (!File.Exists(vbsPath))
                throw new FileNotFoundException("未找到重启脚本 restart.vbs", vbsPath);
            if (exePath == null || !File.Exists(exePath))
                throw new FileNotFoundException("无法定位当前程序路径", exePath);

            // 启动VBS脚本（隐藏窗口）
            var startInfo = new ProcessStartInfo
            {
                FileName = "wscript.exe",
                Arguments = $"\"{vbsPath}\" \"{exePath}\"", // 传递exe路径给VBS
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            if (Process.Start(startInfo) == null)
                throw new Win32Exception("启动VBS脚本失败");

            AddStatusText("重启脚本已执行，即将关闭当前程序...");

            // 立即关闭应用程序
            _ = ShutDownApp();
        }
        catch (Win32Exception ex)
        {
            HandleRestartError(ex.NativeErrorCode switch
            {
                0x00000002 => "找不到VBS脚本或程序文件",
                0x00000005 => "权限不足无法执行脚本",
                _ => $"系统错误 (0x{ex.NativeErrorCode:X8})"
            }, ex);
        }
        catch (Exception ex)
        {
            HandleRestartError("启动过程中发生意外错误", ex);
        }
    }

    private async Task ShutDownApp()
    {
        FlushStatusBuffer(); // 确保所有日志已输出
        await Task.Delay(1000);
        await SaveLogger(); // 保存日志
        Application.Current.Dispatcher.Invoke(Application.Current.Shutdown); // 关闭程序
        await Task.Delay(1000);
    }

    private void HandleRestartError(string errorType, Exception ex)
    {
        var errorMessage = $"重启失败 [{errorType}]: {ex.Message}";
        AddStatusText($"× {errorMessage}");

        MessageBox.Show(
            $"{errorMessage}\n请手动重启应用程序",
            "重启错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );

        // 紧急关闭
        _ = ShutDownApp();
    }


    private void UploadLocalInfo()
    {
        LocalInfo.Version = _serverVersion;
        LocalInfo.Id = _serverId;
        LocalInfo.Name = _serverName;
        LocalInfo.StartupFile = _serverStartupFile;
        LocalInfo.CreateTime = _serverCreateTime;
        LocalInfo.SaveLocalInfo();
    }

    private async Task SaveLogger()
    {
        try
        {
            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "log");
            Directory.CreateDirectory(logDir);
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HH+mm+ss");
            var logPath = Path.Combine(logDir, $"{timestamp}.log");
            await using var writer = new StreamWriter(logPath);
            await writer.WriteAsync(StatusText.Text);
            AddStatusText($"更新日志已保存到: {logPath}");
        }
        catch (Exception ex)
        {
            AddStatusText($"日志保存失败: {ex.Message}");
            // 尝试创建错误日志
            try
            {
                Directory.CreateDirectory("log/err");
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HH+mm+ss");
                var errorLog = Path.Combine(Directory.GetCurrentDirectory(), $"log/err/error({timestamp}).log");
                await File.AppendAllTextAsync(errorLog,
                    $"[{DateTime.Now:u}] 日志保存错误: {ex}\n{StatusText.Text}\n\n");
            }
            catch
            {
                // ignored
            }
        }
    }

    /**
     * 更新文件
     */
    private async Task UpdateFiles(dynamic fileList)
    {
        UpdateProgress.Value = 20;
        if (fileList is not JObject jObject)
        {
            AddStatusText("无效的文件列表格式");
            return;
        }

        const int bufferSize = 81920;
        var serverBaseUrl = $"{LocalInfo.ServerUrl}/static/upload/{_serverName}-{_serverVersion}/";
        var localRoot = Path.Combine(Directory.GetCurrentDirectory(), _serverName);

        try
        {
            AddStatusText("开始文件更新流程");

            // 正确获取文件总数
            var totalFiles = jObject.Properties().Count();
            if (totalFiles == 0)
            {
                AddStatusText("未发现需更新文件");
                UpdateProgress.Value = 100;
                return;
            }

            var progressI = 0;
            // 缓存优化机制
            var totalSteps = totalFiles * 1.0;
            var lastProgress = 20.0;
            foreach (var prop in jObject.Properties())
                try
                {
                    var fileName = Path.GetFileName(prop.Name);
                    if (fileName == ".lock") continue; // 忽略文件
                    var relativePath = prop.Name.Replace('/', '\\');
                    var serverMd5 = prop.Value.ToString().ToLower();
                    var localPath = Path.Combine(localRoot, relativePath);
                    var pathSegment = prop.Name.Replace('\\', '/'); // 将反斜杠统一转为正斜杠
                    var baseUri = new Uri(serverBaseUrl);
                    var downloadUrl = new Uri(baseUri, pathSegment); // 使用Uri的路径组合功能
                    if (await NeedUpdateAsync(localPath, serverMd5))
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
                finally
                {
                    // progressI++;
                    // if (progressI % 3 == 0) // 每处理3个文件释放一次UI线程
                    //     await Task.Delay(200); // 适当延迟让出UI线程
                    // var progress = totalFiles > 0
                    //     ? progressI * 1.0 / totalFiles
                    //     : 1.0;
                    // UpdateProgress.Value = 20 + progress * 100 * 0.8;
                    progressI++;

                    // 智能进度更新策略
                    var currentProgress = 20 + progressI / totalSteps * 80;
                    if (currentProgress - lastProgress > 1.5 ||
                        (DateTime.Now - _lastProgressUpdate).TotalMilliseconds > 150)
                    {
                        UpdateProgress.Value = currentProgress;
                        lastProgress = currentProgress;
                        _lastProgressUpdate = DateTime.Now;

                        // 每处理50个文件强制GC回收
                        if (progressI % 50 == 0)
                        {
                            await Task.Delay(1);
                            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
                        }
                    }
                }

            AddStatusText($"更新完成 ({totalFiles} 个文件已处理)");
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
                await using var sourceStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);

                var buffer = new byte[bufferSize];
                int read;

                while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    await fileStream.WriteAsync(buffer, 0, read);
                return; // 下载成功
            }
            catch (Exception ex) when (i < retries)
            {
                AddStatusText($"下载地址：{url}");
                AddStatusText($"下载失败 ({i}/{retries}): {ex.Message}");
                await Task.Delay(3000 * i);
            }

        throw new Exception($"文件下载失败: {Path.GetFileName(savePath)}");
    }

    // 检查是否需要从网络更新
    private async Task<bool> NeedUpdateAsync(string localPath, string serverMd5)
    {
        try
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(localPath)) return true;

                using var md5 = MD5.Create();
                using var stream =
                    new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var hashBytes = md5.ComputeHash(stream);
                var localHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                return !localHash.Equals(serverMd5);
            });
        }
        catch (Exception ex)
        {
            AddStatusText($"校验失败 [{localPath}]: {ex.Message}");
            return true;
        }
    }

    /**
     * 删除多余的文件和目录
     */
    private async Task DeleteUnnecessaryFiles(dynamic fileList)
    {
        UpdateProgress.Value = 10;
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

            UpdateProgress.Value = 12;
            // 合并有效路径（文件+必须保留的目录）
            var validPaths = new HashSet<string>(serverPaths
                    .Concat(serverDirectories),
                StringComparer.OrdinalIgnoreCase
            );

            var localRoot = Path.Combine(Directory.GetCurrentDirectory(), _serverName);
            AddStatusText($"开始深度清理，根目录: {localRoot}");

            // 获取本地所有路径（文件+目录）
            var allLocalPaths = new List<string>();

            // 文件路径
            foreach (var file in Directory.EnumerateFiles(localRoot, "*", SearchOption.AllDirectories))
            {
                var relPath = GetRelativePath(file, localRoot);
                allLocalPaths.Add(relPath);
            }

            UpdateProgress.Value = 14;
            // 目录路径（包含空目录）
            foreach (var dir in Directory.EnumerateDirectories(localRoot, "*", SearchOption.AllDirectories))
            {
                var relPath = GetRelativePath(dir, localRoot) + "/"; // 目录标识
                allLocalPaths.Add(relPath);
            }

            UpdateProgress.Value = 16;
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

            UpdateProgress.Value = 18;
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

            UpdateProgress.Value = 20;
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
        UpdateProgress.Value = 0;
        try
        {
            // 构建请求URL
            var serverFileListUrl = LocalInfo.ServerUrl + $"/static/upload/{_serverName}-{_serverVersion}.json";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ClientApp/1.0");

            UpdateProgress.Value = 5;
            var response = await client.GetAsync(serverFileListUrl);
            if (!response.IsSuccessStatusCode)
            {
                AddStatusText($"获取服务器文件列表请求失败：{response.StatusCode}");
                return null;
            }

            UpdateProgress.Value = 8;
            var jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonResponse) ?? throw new InvalidOperationException();
            if (data == null) Instance.AddStatusText("获取服务器文件列表请求失败!");
            UpdateProgress.Value = 10;
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
                Task.Delay(800).ContinueWith(__ => { _ = ShutDownApp(); });
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
            AddStatusText("关闭所有程序");
            // 延迟关闭当前程序
            Task.Delay(1000).ContinueWith(__ => { _ = ShutDownApp(); });
        }
    }

    /**
     * 检查更新 ,返回Flase则不用更新，True则需要更新
     */
    private async Task<bool> CheckUpdates()
    {
        try
        {
            var server = LocalInfo.ServerUrl;
            var version = LocalInfo.Version;
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
                await ShutDownApp();
                return false;
            }

            // 解析 JSON
            var content = await response.Content.ReadAsStringAsync();

            dynamic json = JsonConvert.DeserializeObject(content)!;
            if (json.code != 0 || json.data?.version == null)
            {
                AddStatusText("无效的服务器响应格式!");
                AddStatusText("检查更新失败！请重试！");
                await ShutDownApp();
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
            AddStatusText($"检查更新失败！{ex.Message}");
            Show();
            await ShutDownApp();
            return false;
        }
    }

    private void StatusText_TextChanged(object sender, SizeChangedEventArgs e)
    {
        StatusScrollViewer.ScrollToEnd();
    }


    // public void AddStatusText(string text)
    // {
    //     lock (_logBuffer)
    //     {
    //         Console.WriteLine(text);
    //         _logBuffer.AppendLine(text);
    //
    //         // 缓冲控制：每200ms或满50行刷新
    //         if (_logBuffer.Length > 50 * 100 ||
    //             (DateTime.Now - _lastLogFlush).TotalMilliseconds > 200)
    //         {
    //             var logs = _logBuffer.ToString();
    //             _logBuffer.Clear();
    //
    //             Dispatcher.InvokeAsync(() =>
    //             {
    //                 StatusText.AppendText(logs);
    //
    //                 // 智能滚动控制
    //                 if (StatusText.LineCount > 0)
    //                 {
    //                     var rect = StatusText.GetRectFromCharacterIndex(StatusText.Text.Length - 1);
    //                     StatusScrollViewer.ScrollToVerticalOffset(rect.Bottom);
    //                 }
    //             });
    //
    //             _lastLogFlush = DateTime.Now;
    //         }
    //     }
    // }

    private void FlushStatusBuffer()
    {
        lock (_logBuffer)
        {
            if (_logBuffer.Length == 0) return;

            var logs = _logBuffer.ToString();
            _logBuffer.Clear();

            Dispatcher.InvokeAsync(() =>
            {
                StatusText.AppendText(logs);

                // 精准滚动控制
                if (StatusText.LineCount > 0)
                {
                    var rect = StatusText.GetRectFromCharacterIndex(StatusText.Text.Length - 1);
                    StatusScrollViewer.ScrollToVerticalOffset(rect.Bottom);
                }
            }, DispatcherPriority.Background);

            _lastLogFlush = DateTime.Now;
        }
    }

// 修改现有AddStatusText方法
    public void AddStatusText(string text)
    {
        lock (_logBuffer)
        {
            Console.WriteLine(text);
            _logBuffer.AppendLine(text);

            // 强制刷新条件（关键错误立即显示）
            if (text.StartsWith("×") || text.Contains("错误"))
            {
                FlushStatusBuffer();
                return;
            }

            // 自动刷新条件
            if (_logBuffer.Length > 50 * 100 ||
                (DateTime.Now - _lastLogFlush).TotalMilliseconds > 200)
                FlushStatusBuffer();
        }
    }
}