using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace Server.Controller;

[Route("api/[controller]")]
[ApiController]
public class VersionController : ControllerBase
{
    /**
     * 获取当前最新版本号
     */
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // 构建数据库路径（相对于项目根目录）
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "version.db");
        if (!System.IO.File.Exists(dbPath)) return NotFound(new { error = "数据库文件不存在" });
        var connectionString = $"Data Source={dbPath}";
        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, name, version, createTime 
                FROM versions 
                ORDER BY createTime DESC 
                LIMIT 1";

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return NotFound(new { error = "没有找到任何版本记录" });
            var result = new
            {
                code = 0,
                msg = "",
                data = new
                {
                    id = reader.GetInt32(reader.GetOrdinal("id")),
                    name = reader.GetString(reader.GetOrdinal("name")),
                    version = reader.GetString(reader.GetOrdinal("version")),
                    createTime = reader.GetDateTime(reader.GetOrdinal("createTime"))
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "读取数据库时出错", details = ex.Message });
        }
    }
}