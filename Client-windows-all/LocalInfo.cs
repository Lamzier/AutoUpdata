using System.IO;
using Newtonsoft.Json;

namespace Client_windows_all;

public static class LocalInfo
{
    // 初始化
    static LocalInfo()
    {
        GetLocalInfo();
    }

    // 服务器地址
    private static string ServerUrl { get; set; } = "http://localhost:5000";

    // 当前版本号
    private static string Version { get; set; } = "0.0.0";

    private static string StartupFile { get; set; } = "start.exe";

    private static DateTime CreateTime { get; set; }

    private static int Id { get; set; } = -1;

    public static string GetStartupFile()
    {
        return StartupFile;
    }

    public static string GetServerUrl()
    {
        return ServerUrl;
    }

    public static string GetVersion()
    {
        return Version;
    }

    private static void SaveLocalInfo()
    {
        try
        {
            var config = new
            {
                Id, ServerUrl, Version, StartupFile, CreateTime
            };
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "info.json");
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.AddStatusText($"写入 info.json 失败：{ex.Message}");
        }

        MainWindow.Instance.AddStatusText("写入 info.json 成功!");
    }

    private static void GetLocalInfo()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "info.json");

        if (!File.Exists(filePath))
        {
            SaveLocalInfo();
            MainWindow.Instance.AddStatusText("未找到 info.json，已生成默认配置。");
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            dynamic data = JsonConvert.DeserializeObject(json)!;
            ServerUrl = data.ServerUrl ?? "http://localhost:5000";
            Version = data.Version ?? "1.0.0";
            Id = data.Id;
            StartupFile = data.StartupFile ?? "start.exe";
            CreateTime = data.CreateTime;
            MainWindow.Instance.AddStatusText("成功加载 info.json 配置。");
            MainWindow.Instance.AddStatusText($"服务器地址：{ServerUrl}");
            MainWindow.Instance.AddStatusText($"当前版本号：{Version}");
        }
        catch
        {
            SaveLocalInfo();
            MainWindow.Instance.AddStatusText("info.json 格式错误，使用默认配置。");
        }
    }
}