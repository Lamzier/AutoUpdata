using Microsoft.Extensions.FileProviders;
using Server.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers(); // 添加MVC

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

// 使用静态资源
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "upload")),
    RequestPath = "/static/upload"
});
app.UseAntiforgery();

// 使用中间件
app.UseAuthorization();
app.MapControllers(); // 映射Api控制器路由

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();