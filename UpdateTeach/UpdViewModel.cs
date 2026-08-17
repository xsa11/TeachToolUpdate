using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using System.Net;


namespace UpdateTeach
{
    public class UpdViewModel
    {
        public ShellStream shellStream;
        public async Task<string> ConnectSSH(string host, int port, string username, string password)
        {

            // 创建SSH客户端实例
            using (var client = new SshClient(host, port, username, password))
            {
                try
                {
                    if (client.IsConnected)
                    {
                        client.Disconnect();
                    }
                    // 连接服务器
                    client.Connect();

                    shellStream = client.CreateShellStream("xterm", 80, 24, 800, 600, 1024);
                    // 执行 cd 命令（改变目录）
                    string cmd1 = "cd /userfs/app && ps | grep OIMAUI | grep -v grep | awk '{print $1}' | xargs kill";
                    //string cmd2 = "cd /userfs/app  && export LD_LIBRARY_PATH=$PWD && nohup ./OIMAUI  ";//1>oima_std.log 2>oima_err.log将日志分别写入到这两个文件中

                    shellStream.WriteLine(cmd1);
                    Thread.Sleep(500);
                    //shellStream.WriteLine(cmd2);
                    Thread.Sleep(500);
                    return "重启示教器完成";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
                finally
                {
                    // 断开连接
                    //client.Disconnect();
                }
            }
        }

        public async Task<List<string>> ScanAsync(string baseIp, int start = 1, int end = 254)
        {
            List<Task<string>> pingTasks = new List<Task<string>>();

            for (int i = start; i <= end; i++)
            {
                string ip = $"{baseIp}{i}";
                pingTasks.Add(PingSingleIpAsync(ip));
            }

            var results = await Task.WhenAll(pingTasks);
            List<string> onlineIps = new List<string>();
            foreach (var ip in results)
            {
                if (!string.IsNullOrEmpty(ip))
                    onlineIps.Add(ip);
            }

            // 过滤本机所有IP
            var localIpList = GetAllLocalIpV4();
            // 返回不包含本机IP的集合
            return onlineIps.Where(ip => !localIpList.Contains(ip)).ToList();
        }


        private async Task<string> PingSingleIpAsync(string ipAddress, int timeoutMs = 150)
        {
            try
            {
                using Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync(ipAddress, timeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    return ipAddress;
                }
            }
            catch
            {
                // ping异常（无权限/防火墙拦截）直接忽略
            }
            return string.Empty;
        }


        public List<string> GetAllLocalIpV4()
        {
            List<string> ips = new List<string>();
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                // 筛选IPv4，排除本地回环127.0.0.1
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(ip))
                {
                    ips.Add(ip.ToString());
                }
            }
            return ips;
        }
    }
}

