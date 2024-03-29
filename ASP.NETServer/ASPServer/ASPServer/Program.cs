using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
               webBuilder.UseStartup<Startup>();
               webBuilder.UseUrls("http://127.0.0.1:5000");
           })
           .ConfigureLogging((hostingContext, logging) =>
           {
               logging.ClearProviders();
               logging.AddConsole(options =>
               {
                   options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
                   options.LogToStandardErrorThreshold = LogLevel.Information;
               });
           });
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        // Add JWT authentication services
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("te9pTxuphH0nWwsFDT0VEdmDpT2k1j2k")), // Replace with your secret key
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Add other services
        services.AddLogging(builder => builder.AddConsole());

        // Register the IBlacklistService
        services.AddSingleton<IBlacklistService, BlacklistService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Use authentication middleware
        app.UseAuthentication();

        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

[ApiController]
[Route("Auth")]
public class AuthController : ControllerBase
{
    private const string ExpectedUsername = "123";
    private const string ExpectedPassword = "123";
    private const string SecretKey = "te9pTxuphH0nWwsFDT0VEdmDpT2k1j2k";
    private readonly SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

    private readonly IBlacklistService _blacklistService;

    public AuthController(IBlacklistService blacklistService)
    {
        _blacklistService = blacklistService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request != null)
        {
            if (request.Username == ExpectedUsername && request.Password == ExpectedPassword)
            {
                var message = "登录成功";
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

    [Authorize]
    [HttpPost("secure")]
    public IActionResult SecureEndpoint()
    {
        var message = "Secure endpoint accessed successfully";
        return Ok(new { Message = message });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // 从当前用户的 Claims 中获取 Subject (UserID)
        var userId = User.FindFirst(ClaimTypes.Name)?.Value;

        // 将当前 Token 加入黑名单
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        _blacklistService.AddToBlacklist(userId, token);

        var message = "登出成功";
        return Ok(new { Success = true, Message = message });
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
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return tokenString;
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

public interface IBlacklistService
{
    void AddToBlacklist(string userId, string token);
}

public class BlacklistService : IBlacklistService
{
    private readonly IDictionary<string, HashSet<string>> _blacklist = new Dictionary<string, HashSet<string>>();

    public void AddToBlacklist(string userId, string token)
    {
        if (!_blacklist.ContainsKey(userId))
        {
            _blacklist[userId] = new HashSet<string>();
        }

        _blacklist[userId].Add(token);
    }

    public bool IsTokenBlacklisted(string userId, string token)
    {
        return _blacklist.ContainsKey(userId) && _blacklist[userId].Contains(token);
    }
}