using QRCoder;

namespace Server.Hosted;

public class StartupTaskService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    { 
        _ = ShowCode();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ShowCode()
    {
        await Task.Delay(5000);
        Console.WriteLine();
        // 设置控制台默认背景为黑色
        Console.BackgroundColor = ConsoleColor.Black;

        const string weChatPayUrl = "wxp://f2f0Iz-n-DfzcQSi_swO9xbI6UPD_6lV5uXZeaPpyXcgabI";
        const string aliPayUrl = "https://qr.alipay.com/fkx19656sxqgctennrso749";

        using var qrGenerator = new QRCodeGenerator();

        var qrCodeDataWeChat = qrGenerator.CreateQrCode(weChatPayUrl, QRCodeGenerator.ECCLevel.M);
        var qrCodeDataAliPay = qrGenerator.CreateQrCode(aliPayUrl, QRCodeGenerator.ECCLevel.M);

        PrintSideBySide(qrCodeDataWeChat, qrCodeDataAliPay);
        Console.ResetColor();
        Console.WriteLine("来扫我呀~~~");
    }

    private void PrintSideBySide(QRCodeData data1, QRCodeData data2)
    {
        const string moduleChar = " "; // 使用单个空格代替两个空格
        var totalRows = Math.Max(data1.ModuleMatrix.Count, data2.ModuleMatrix.Count);

        for (var y = 0; y < totalRows; y++)
        {
            // 打印第一个二维码的一行
            if (y < data1.ModuleMatrix.Count)
                foreach (bool isDark in data1.ModuleMatrix[y])
                    if (isDark)
                    {
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.Write(moduleChar);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.Write(moduleChar);
                        Console.ResetColor();
                    }

            Console.Write(" "); // 减少间隔字符

            if (y < data2.ModuleMatrix.Count)
                foreach (bool isDark in data2.ModuleMatrix[y])
                    if (isDark)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.Write(moduleChar);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.Write(moduleChar);
                        Console.ResetColor();
                    }

            Console.WriteLine(); // 换行
            Console.ResetColor();
        }
    }
}