using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ServiceKeeper.UI
{
    public class ServiceKeeperUIOptions
    {
        /// <summary>
        /// 获取或设置用于访问 ServiceKeeper-ui 的路由前缀
        /// </summary>
        public string RoutePrefix { get; set; } = "ServiceKeeper";
        private byte[]? jwtSecretKey;
        /// <summary>
        /// 用于登陆验证
        /// </summary>
        public byte[]? JwtSecretKey
        {
            get
            {
                return jwtSecretKey;
            }
            private set
            {
                jwtSecretKey = value;
            }
        }
        /// <summary>
        /// 创建64位Key
        /// </summary>
        /// <param name="originalKey"></param>
        public void CreateJwtSecretKey(string originalKey)
        {
            if (jwtSecretKey != null) return;
            byte[] salt = Array.Empty<byte>(); // No salt

            using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(originalKey));
            using Rfc2898DeriveBytes hkdf = new(originalKey, salt, 10000, HashAlgorithmName.SHA256);
            JwtSecretKey = hkdf.GetBytes(64); // Derive a 64-byte key
        }
        /// <summary>
        /// 获取或设置用于检索 ServiceKeeper-ui 页面的 Stream 函数
        /// </summary>
        public Func<Stream> ProducerStream { get; set; } = () => typeof(ServiceKeeperUIOptions).Assembly
            .GetManifestResourceStream("ServiceKeeper.UI.producer-index.html")!;
        /// <summary>
        /// 获取或设置用于检索 ServiceKeeper-ui 页面的 Stream 函数
        /// </summary>
        public Func<Stream> ConsumerStream { get; set; } = () => typeof(ServiceKeeperUIOptions).Assembly
            .GetManifestResourceStream("ServiceKeeper.UI.consumer-index.html")!;
        /// <summary>
        /// 获取或设置允许的用户名和密码
        /// key:用户名 value:密码
        /// </summary>
        public Dictionary<string, string> AllowedCredentials { get; set; } = new Dictionary<string, string>();
    }

    //public Func<Stream> IndexStream { get; set; } = () => typeof(SwaggerUIOptions).GetTypeInfo().Assembly
    //.GetManifestResourceStream("Swashbuckle.AspNetCore.SwaggerUI.index.html");
}
