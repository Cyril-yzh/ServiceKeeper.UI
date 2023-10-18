using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ServiceKeeper.Core;
using ServiceKeeper.Core.MediatR;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using MediatR;
using System.Threading.Tasks;
using ServiceKeeper.UI.MediatR;
using ServiceKeeper.UI.Utils;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace ServiceKeeper.UI.Middleware
{
    public class IndexMiddleware
    {
        private readonly IOptions<ServiceKeeperUIOptions> _options;
        private readonly ServiceRole _role;
        private readonly ServiceRegistry _registry;
        private readonly ServiceScheduler _scheduler;
        private readonly LogLoader _logLoader;
        private readonly IMediator _mediator;
        private readonly RequestDelegate next;
        private readonly bool _isNeedJwt = false;

        public IndexMiddleware(IOptions<ServiceKeeperUIOptions> options, ServiceRegistry serviceRegistry, RequestDelegate next, ServiceScheduler scheduler, LogLoader logLoader, IMediator mediator)
        {
            _options = options;
            _registry = serviceRegistry;
            _role = _registry.CurrentOptions.ServiceRole;
            _scheduler = scheduler;
            _logLoader = logLoader;
            _mediator = mediator;
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
            if (httpMethod == "GET" && Regex.IsMatch(path, $"^/{Regex.Escape("ServiceKeeper")}/?index.html$", RegexOptions.IgnoreCase))
            {
                await HandleGetIndexHtmlAsync(context.Response);
                return;
            }
            if (httpMethod == "GET" && Regex.IsMatch(path, $"^/{Regex.Escape("ServiceKeeper")}/?getJS$", RegexOptions.IgnoreCase))
            {
                await HandleGetModifyJSAsync(context);
                return;
            }
            if (httpMethod == "GET" && Regex.IsMatch(path, $"^/{Regex.Escape("ServiceKeeper")}/?getCSS$", RegexOptions.IgnoreCase))
            {
                await HandleGetCSSAsync(context);
                return;
            }

            // 处理登陆请求,返回 token
            if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape("ServiceKeeper")}/login/?$", RegexOptions.IgnoreCase))
            {
                await HandleLoginAsync(context);
                return;
            }
            // 处理初始化 SignalR 请求
            if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape("ServiceKeeper")}/Hub/negotiate", RegexOptions.IgnoreCase))
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


            //添加任务
            if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape("ServiceKeeper")}/AddTask/?$", RegexOptions.IgnoreCase))
            {
                await ValidTokenAndHandleRequest(context, async (context) => { await HandleAddTaskAsync(context); });
                return;
            }
            // 删除任务
            if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape("ServiceKeeper")}/DeleteTask/?$", RegexOptions.IgnoreCase))
            {
                await ValidTokenAndHandleRequest(context, async (context) => { await HandleDeleteTaskAsync(context); });
                return;
            }
            //获取日志
            if (httpMethod == "POST" && Regex.IsMatch(path, $"^/{Regex.Escape("ServiceKeeper")}/GetLog/?$", RegexOptions.IgnoreCase))
            {
                await ValidTokenAndHandleRequest(context, async (context) => { await HandleGetLogAsync(context); });
                return;
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
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);
            if (jsonData == null || !jsonData.TryGetValue("username", out var username) || !jsonData.TryGetValue("password", out var password))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("请求数据无效");
                _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, "ServiceKeeperUI:登陆时请求数据无效"));
                return;
            }

            // 检查提供的用户名和密码是否被允许
            if (_options.Value.AllowedCredentials.TryGetValue(username, out var expectedPassword) && password == expectedPassword)
            {
                var tokenPayload = new Dictionary<string, object>
                {
                    { "username", username },
                };

                // 序列化并签署令牌，将其作为字符串返回
                var token = JwtTokenGenerator.GenerateToken(tokenPayload, _options.Value.JwtSecretKey!, _options.Value.JWTExpirationTime);

                //将token返回给前端
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { token }));
                _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"ServiceKeeperUI:用户'{username}'登陆成功"));
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("账号密码错误!");
                _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"ServiceKeeperUI:用户'{username}'账号密码错误"));
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

        /// <summary>
        /// 处理添加任务请求
        /// </summary>
        private async Task HandleAddTaskAsync(HttpContext context)
        {
            try
            {
                TaskEntity? jsonData;
                // 读取请求体
                var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                // 解析前端创建的任务
                jsonData = JsonConvert.DeserializeObject<TaskEntity>(requestBody);

                if (jsonData != null)
                {
                    _scheduler.AddTask(jsonData);
                    context.Response.StatusCode = 200;
                    _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"ServiceKeeperUI:任务'{jsonData.Detail.Name}'添加成功"));
                    await context.Response.WriteAsync("任务添加成功");
                }
                else
                {
                    // 如果无法解析请求体中的任务数据，返回错误响应
                    context.Response.StatusCode = 400;
                    _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"ServiceKeeperUI:添加任务时无法解析请求体中的任务数据,任务添加失败"));
                    await context.Response.WriteAsync("无法解析请求体中的任务数据");
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"ServiceKeeperUI:添加任务时发生了服务器错误:'{ex.Message}',任务添加失败"));
                await context.Response.WriteAsync("发生了服务器错误:" + ex.Message);
            }
        }

        /// <summary>
        /// 处理删除任务请求
        /// </summary>
        private async Task HandleDeleteTaskAsync(HttpContext context)
        {
            try
            {
                var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                if (Guid.TryParse(requestBody, out Guid id))
                {
                    _scheduler.DeleteTask(id, true);
                    context.Response.StatusCode = 200;
                    _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"ServiceKeeperUI:任务'{id}'任务删除成功"));
                    await context.Response.WriteAsync("任务删除成功");
                }
                else
                {
                    context.Response.StatusCode = 400;
                    _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"ServiceKeeperUI:添加任务时无法解析请求体中的任务数据,任务添加失败"));
                    await context.Response.WriteAsync("无效的任务ID");
                }


            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                _ = _mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"ServiceKeeperUI:删除任任务时发生了服务器错误:'{ex.Message}',任务添加失败"));
                await context.Response.WriteAsync("删除任务时发生了服务器错误：" + ex.Message);
            }
        }

        /// <summary>
        /// 处理获取日志请求
        /// </summary>
        private async Task HandleGetLogAsync(HttpContext context)
        {
            try
            {
                var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                string? date = JsonConvert.DeserializeObject<string>(requestBody);
                if (date != null)
                {
                    List<string>? result = _logLoader.LoadLog(date);
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new { result }));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("传入的参数有误,无法获取日志信息");
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("发生了服务器错误：" + ex.Message);
            }
        }

        /// <summary>
        ///  根据需要修改index.html的逻辑，例如读取配置，替换占位符等
        /// </summary>
        private async Task HandleGetIndexHtmlAsync(HttpResponse response)
        {
            response.StatusCode = 200;
            // 设置Content-Type头部为"text/html"
            response.ContentType = "text/html;charset=utf-8";

            using Stream stream = _role switch
            {
                ServiceRole.Producer => _options.Value.ProducerStream(),
                //ServiceRole.Consumer => _options.Value.ConsumerStream(),
                _ => throw new Exception()
            };
            using var reader = new StreamReader(stream);
            var jsBuilder = new StringBuilder(await reader.ReadToEndAsync());
            jsBuilder.Replace("serviceKeeper_baseUrl", _options.Value.ServiceKeeperUrl);

            // 输出修改后的index.html
            await response.WriteAsync(jsBuilder.ToString(), Encoding.UTF8);
        }

        private async Task HandleGetModifyJSAsync(HttpContext context)
        {
            if (context.Request.Path.HasValue && context.Request.Query.ContainsKey("file"))
            {
                string filename = context.Request.Query["file"].ToString();
                string resourceName = "ServiceKeeper.UI.Vue.js." + filename;
                using Stream? resourceStream = typeof(ServiceKeeperUIOptions).Assembly.GetManifestResourceStream(resourceName);
                {
                    if (resourceStream != null)
                    {
                        context.Response.ContentType = "application/javascript";
                        using var reader = new StreamReader(resourceStream);
                        var jsBuilder = new StringBuilder(await reader.ReadToEndAsync());
                        jsBuilder.Replace("serviceKeeper_baseUrl", $"\"{_options.Value.ServiceKeeperUrl}\"");
                        jsBuilder.Replace("serviceKeeper_needCredentials", _isNeedJwt.ToString().ToLower());
                        //jsBuilder.Replace("%BASE_URL%", "http://your_dynamic_base_url_here");

                        // 输出修改后的index.html
                        await context.Response.WriteAsync(jsBuilder.ToString(), Encoding.UTF8);
                    }
                    else
                    {
                        context.Response.StatusCode = 404; // Not Found
                    }
                }
                return;
            }
        }

        private static async Task HandleGetCSSAsync(HttpContext context)
        {
            if (context.Request.Path.HasValue && context.Request.Query.ContainsKey("file"))
            {
                string filename = context.Request.Query["file"].ToString();
                string resourceName = "ServiceKeeper.UI.Vue.css." + filename;
                using Stream? resourceStream = typeof(ServiceKeeperUIOptions).Assembly.GetManifestResourceStream(resourceName);
                {
                    if (resourceStream != null)
                    {
                        context.Response.ContentType = "text/css";
                        await resourceStream.CopyToAsync(context.Response.Body);
                    }
                    else
                    {
                        context.Response.StatusCode = 404; // Not Found
                    }
                }
                return;
            }
        }
    }
}

