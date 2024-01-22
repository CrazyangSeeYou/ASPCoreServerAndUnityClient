using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Console;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

#region 启动后台服务

/// <summary>
/// 启动服务
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
       Host.CreateDefaultBuilder(args)
           .ConfigureWebHostDefaults(webBuilder =>
           {
               //开启服务，以指定的IP和端口
               webBuilder.UseStartup<Startup>();
               webBuilder.UseUrls("http://127.0.0.1:5000");
           })
           .ConfigureLogging((hostingContext, logging) =>
           {
               logging.ClearProviders();
               logging.AddConsole(options =>
               {
                   //在控制台显示调试的信息
                   //options.IncludeScopes = true;
                   options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
                   options.LogToStandardErrorThreshold = LogLevel.Information; // Adjust log level as needed
               });
           });
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddLogging(builder => builder.AddConsole());
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

#endregion 启动后台服务

#region 登录验证测试

/// <summary>
/// 登录验证相关代码
/// </summary>
[ApiController]
[Route("Auth")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    private const string ExpectedUsername = "123";
    private const string ExpectedPassword = "123";

    private const string SecretKey = "fgA/VDnnRkyKAv3jHc2cv8vtksWmJssm+K"; // Replace with a secure secret key
    private readonly SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation($"收到登录请求. Username: {request.Username}, Password: {request.Password}");

        if (request != null)
        {
            if (request.Username == ExpectedUsername && request.Password == ExpectedPassword)
            {
                var message = "登录成功";
                //var response = new LoginResponse { Success = true, Message = message };

                var token = GenerateToken();
                var response = new LoginResponse { Success = true, Message = message, Token = token };
                return Ok(response);
            }
            else
            {
                var message = "用户名或密码错误";
                var response = new LoginResponse { Success = false, Message = message };
                return Ok(response);
            }
        }
        else
        {
            var message = "缺少用户名或密码";
            var response = new LoginResponse { Success = false, Message = message };
            return Ok(response);
        }
    }

    private string GenerateToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
            new Claim(ClaimTypes.Name, ExpectedUsername)
                // Add additional claims as needed
            }),
            Expires = DateTime.UtcNow.AddHours(1), // Token expiration time
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            // Use SecurityAlgorithms.HmacSha256 instead of SecurityAlgorithms.HmacSha256Signature
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public string Token { get; set; }
    }
}

#endregion 登录验证测试