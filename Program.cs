using Serilog.Events;
using Serilog;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using System.Numerics;

class Program
{
    private static IConfiguration Configuration => new ConfigurationBuilder()
              .SetBasePath(AppContext.BaseDirectory)
              .AddJsonFile("ipSetting.json", optional: false).Build();

    static async Task Main(string[] args)
    {
        // 配置日志
        ConfigureLogging();
        try
        {
            //获取线体
            var pdline = Get<string>("Identity", "");
            var ipIdentify = "Ips";
            switch (pdline)
            {
                case "0":
                    ipIdentify = "Ips";
                    break;
                case "1":
                    ipIdentify = "Ips1";
                    break;
                case "2":
                    ipIdentify = "Ips2";
                    break;
            }
            // 获取所有配置的ip地址
            var ipAddresses = Get<List<string>>(ipIdentify, new List<string>());
            // 创建一个Ping实例
            while (true)
            {
                // 使用并行处理来Ping所有IP地址
                await Task.WhenAll(ipAddresses.Select(ipAddress => PingAsync(ipAddress)));
                // 休眠一定时间（例如，1分钟）后再继续Ping
                //await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
        catch (Exception ex)
        {
            Log.Fatal($"错误：{ex.Message}");
        }
    }

    static async Task PingAsync(string ipAddress)
    {
        using var ping = new Ping();
        try
        {
            var reply = await ping.SendPingAsync(ipAddress);
            //if (reply.Status==IPStatus.Success)
            //{
            //    Console.WriteLine($"{ipAddress} Ping正常记录");
            //    Log.Information($"{ipAddress} Ping正常记录");
            //}
            //else 
            if (reply.Status == IPStatus.TimedOut)
            {
                Console.WriteLine($"{ipAddress} Ping超时");
                Log.Error($"{ipAddress} Ping超时");
            }
            else if (reply.Status != IPStatus.Success)
            {
                Console.WriteLine($"{ipAddress} Ping失败，状态：{reply.Status}");
                Log.Error($"{ipAddress} Ping失败，状态：{reply.Status}");
            }
            else if (reply.RoundtripTime > 100)
            {
                Console.WriteLine($"警告：{ipAddress} 延时超过100ms");
                Log.Warning($"警告：{ipAddress} 延时超过100ms");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ipAddress} Ping错误：{ex.Message}");
            Log.Fatal($"{ipAddress} Ping错误：{ex.Message}");
        }
    }
    static T Get<T>(string key, T value)
    {
        var result = Configuration.GetSection(key).Get<T>();
        return result ?? value;
    }
    //配置日志
    static void ConfigureLogging()
    {
        try
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                 .MinimumLevel.Information()
                 .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                 .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                  .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
                 .Enrich.FromLogContext()
                 .WriteTo.Logger(log => log.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Debug).
                 WriteTo.File($"{AppContext.BaseDirectory}/Logs/{(DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture) + "/debug.txt")}", fileSizeLimitBytes: 8388608000, rollingInterval: RollingInterval.Day), LogEventLevel.Debug)
                 .WriteTo.Logger(log => log.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Information)
                 .WriteTo.File($"{AppContext.BaseDirectory}/Logs/{(DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture) + "/info.txt")}", fileSizeLimitBytes: 8388608000, rollingInterval: RollingInterval.Day), LogEventLevel.Information)
                 .WriteTo.Logger(log => log.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Warning)
                 .WriteTo.File($"{AppContext.BaseDirectory}/Logs/{(DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture) + "/warning.txt")}", fileSizeLimitBytes: 83886080, rollingInterval: RollingInterval.Day), LogEventLevel.Warning)
                 .WriteTo.Logger(log => log.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Fatal)
                 .WriteTo.File($"{AppContext.BaseDirectory}/Logs/{(DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture) + "/fatal.txt")}", fileSizeLimitBytes: 83886080, rollingInterval: RollingInterval.Day), LogEventLevel.Fatal)
                 .WriteTo.Logger(log => log.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Error)
                 .WriteTo.File($"{AppContext.BaseDirectory}/Logs/{(DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture) + "/error.txt")}", fileSizeLimitBytes: 83886080, rollingInterval: RollingInterval.Day), LogEventLevel.Error)
                 .CreateLogger();
            }
            catch (Exception ex)
            {
                Console.WriteLine("配置输出日志出错，原因：" + ex.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("配置输出日志出错，原因：" + ex.ToString());
        }
    }
}
