using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceKeeper.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ServiceKeeper.UI
{
    public class IndexMiddleware
    {
        private readonly IOptions<ServiceKeeperUIOptions> _options;
        private readonly ServiceRole _role;
        private readonly ServiceRegistry _registry;
        private readonly ServiceTaskScheduler? _scheduler;
        private readonly RequestDelegate next;
        private readonly bool _isNeedJwt = false;

        public IndexMiddleware(IOptions<ServiceKeeperUIOptions> options, ServiceRegistry serviceRegistry, RequestDelegate next, ServiceTaskScheduler? scheduler)
        {
            _options = options;
            _registry = serviceRegistry;
            _role = _registry.CurrentOptions.ServiceRole;
            _scheduler = scheduler;
            if (options.Value.JwtSecretKey != null && options.Value.AllowedCredentials?.Count > 0)
            {
                _isNeedJwt = true;
            }
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var httpMethod = context.Request.Method;
            var path = context.Request.Path.Value;
            if (path == null) return;
            // 处理打开请求,返回 index.html
            if (httpMethod == "GET" && Regex.IsMatch(path, $"^/{Regex.Escape(_options.Value.RoutePrefix)}/?index.html$", RegexOptions.IgnoreCase))
            {
                await ModifyIndexHtmlAsync(context.Response);
            }

            // 处理登陆请求,返回 token
            if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape(_options.Value.RoutePrefix)}/login/?$", RegexOptions.IgnoreCase))
            {
                await HandleLoginAsync(context);
                return;
            }
            // 处理初始化 SignalR 请求
            if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape(_options.Value.RoutePrefix)}/Hub/negotiate", RegexOptions.IgnoreCase))
            {
                if (_isNeedJwt)
                {
                    if (context.Request.Headers.TryGetValue("Authorization", out var tokenHeader) && AuthenticationHeaderValue.TryParse(tokenHeader, out var authHeaderValue))
                    {
                        var token = authHeaderValue.Parameter;

                        // 验证 JWT 令牌
                        if (string.IsNullOrEmpty(token) || !JwtTokenGenerator.IsValidJwtToken(token!, _options.Value.JwtSecretKey!))
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("token不存在或身份验证失败,请重新登陆!");
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("token不存在或身份验证失败,请重新登陆!");
                    }
                }
            }

            // 获取所有服务信息
            //if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape(_options.Value.RoutePrefix)}/Service/?$", RegexOptions.IgnoreCase))
            //{
            //    await ValidTokenAndHandleRequest(context, async (context) => { await HandleGetServicesAsync(context); });
            //    return;
            //}

            //获取所有注册任务信息
            if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape(_options.Value.RoutePrefix)}/Tasks/?$", RegexOptions.IgnoreCase))
            {

            }

            await next(context);
        }

        /// <summary>
        ///  处理登录请求并生成 JWT 令牌
        /// </summary>
        private async Task HandleLoginAsync(HttpContext context)
        {
            // 读取请求体
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();

            // 解析包含用户名和密码的 JSON 数据
            var jsonData = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
            if (jsonData == null || !jsonData.TryGetValue("username", out var username) || !jsonData.TryGetValue("password", out var password))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("请求数据无效");
                return;
            }

            // 检查提供的用户名和密码是否被允许
            if (_options.Value.AllowedCredentials.TryGetValue(username, out var expectedPassword) && password == expectedPassword)
            {
                var tokenPayload = new Dictionary<string, object>
                {
                    { "username", username },
                    // Add any other claims you need
                };

                // 序列化并签署令牌，将其作为字符串返回
                var token = JwtTokenGenerator.GenerateToken(tokenPayload, _options.Value.JwtSecretKey!);

                //将token返回给前端
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { token }));
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid credentials.");
            }
        }

        /// <summary>
        /// 根据是否需要验证身份并处理请求
        /// </summary>
        private async Task ValidTokenAndHandleRequest(HttpContext context, Action<HttpContext> requestHandler)
        {
            if (_isNeedJwt)
            {
                // 从请求标头中提取 JWT 令牌
                if (context.Request.Headers.TryGetValue("Authorization", out var tokenHeader) && AuthenticationHeaderValue.TryParse(tokenHeader, out var authHeaderValue))
                {
                    var token = authHeaderValue.Parameter;

                    // 验证 JWT 令牌
                    if (!string.IsNullOrEmpty(token) && JwtTokenGenerator.IsValidJwtToken(token!, _options.Value.JwtSecretKey!))
                    {
                        requestHandler(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("token不存在或身份验证失败,请重新登陆!");
                    }
                }
            }
            else
            {
                //不需要身份验证,直接执行代码
                requestHandler(context);
            }
        }

        ///// <summary>
        ///// 处理 GetServices 请求
        ///// </summary>
        //private async Task HandleGetServicesAsync(HttpContext context)
        //{
        //    context.Response.StatusCode = 200;
        //    context.Response.ContentType = "application/json";

        //    await context.Response.WriteAsync(JsonConvert.SerializeObject(_registry.GetRegistryAsync().Result));
        //    return;
        //}


        /// <summary>
        ///  根据需要修改index.html的逻辑，例如读取配置，替换占位符等
        /// </summary>
        private async Task ModifyIndexHtmlAsync(HttpResponse response)
        {
            response.StatusCode = 200;
            // 设置Content-Type头部为"text/html"
            response.ContentType = "text/html;charset=utf-8";

            using var stream = _role switch
            {
                ServiceRole.Producer => _options.Value.ProducerStream(),
                ServiceRole.Consumer => _options.Value.ConsumerStream(),
                _ => throw new Exception()
            };

            using var reader = new StreamReader(stream);

            // Inject arguments before writing to response
            var htmlBuilder = new StringBuilder(await reader.ReadToEndAsync());
            //foreach (var entry in GetIndexArguments())
            //{
            //    htmlBuilder.Replace(entry.Key, entry.Value);
            //}
            // 输出修改后的index.html
            await response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);

            // 根据需要进行index.html的修改逻辑，并返回修改后的内容
            // 这里是一个示例，直接返回静态的修改后内容
            //string modifiedHtml = "<html><body><h1>Welcome to Admin UI</h1></body></html>";
            //return Task.FromResult(modifiedHtml);
        }

    }
}

