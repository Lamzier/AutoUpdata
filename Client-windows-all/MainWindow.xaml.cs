using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using Newtonsoft.Json;

namespace Client_windows_all;

public partial class MainWindow
{
    public static MainWindow Instance = null!;

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
        Console.WriteLine("更新");
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