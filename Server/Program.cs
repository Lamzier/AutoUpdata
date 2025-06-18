using Microsoft.Extensions.FileProviders;
using Server.Components;
using Server.Hosted;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers(); // 添加MVC
builder.Services.AddHostedService<StartupTaskService>(); // 添加服务器启动项

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection(); // 禁用Https

app.UseStaticFiles();

var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "upload");
if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);


// 静态资源，允许所有后缀传输
var staticFileOptions = new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "upload")),
    RequestPath = "/static/upload",
    ServeUnknownFileTypes = true, // 允许所有文件类型
    DefaultContentType = "application/octet-stream" // 强制所有文件作为二进制流传输
};
app.UseStaticFiles(staticFileOptions);


app.UseAntiforgery();

// 使用中间件
app.UseAuthorization();
app.MapControllers(); // 映射Api控制器路由

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Urls.Add("http://0.0.0.0:5000"); // 允许所有Ip
app.Run();