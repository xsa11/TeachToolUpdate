using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Renci.SshNet;
using SharpCompress.Archives;
using SharpCompress.Common;
using static System.Net.WebRequestMethods;
using File = System.IO.File;


namespace UpdateTeach
{
    class UpdFTP
    {
        public int progress = 0;
        public async Task<string> ConnectFTP(string host, int port, string username, string password, string remotePath, string localPath,bool IsBackup)
        {

            using (var sftp = new SftpClient(host, port, username, password))
            {

                try
                {
                  
                    string backupPath = "D:\\TeachBackup\\" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
                    await Task.Run(() =>
                       {
                           sftp.Connect();// 连接SFTP服务器
                           // 上传文件夹
                           progress = 20;
                           if (IsBackup)
                           {
                               DownloadDirectory(sftp, remotePath, backupPath);
                           }
                           Thread.Sleep(200);
                           progress = 50;
                           UploadDirectory(sftp, localPath, remotePath);
                           progress = 100;

                       });
                    return "更新示教器成功";
                }
                catch (Exception ex)
                {

                    return ex.Message;

                }
                finally
                {
                    sftp.Disconnect(); // 断开连接

                }
            }

        }

        static void DownloadDirectory(SftpClient sftp, string remoteDir, string localDir)
        {
            // 本地目录不存在则创建
            if (!Directory.Exists(localDir))
                Directory.CreateDirectory(localDir);

            // 遍历远程目录所有条目
            var entries = sftp.ListDirectory(remoteDir);

            foreach (var entry in entries)
            {
                // ⚠️必须过滤 . 和 .. ，防止无限递归
                if (entry.Name == "." || entry.Name == "..")
                    continue;

                string localFullPath = Path.Combine(localDir, entry.Name);

                if (entry.IsRegularFile)
                {
                    // 文件：打开本地文件流进行下载
                    using (var fs = new FileStream(localFullPath, FileMode.Create, FileAccess.Write))
                    {
                        sftp.DownloadFile(entry.FullName, fs);
                    }
                }
                else if (entry.IsDirectory)
                {
                    // 子文件夹：递归下载
                    DownloadDirectory(sftp, entry.FullName, localFullPath);
                }
            }
        }

      public   bool Unrar(string rarFilePath, string extractPath)
        {
            // 校验文件存在
            if (!File.Exists(rarFilePath))
            {
                MessageBox.Show("更新压缩包不存在");
                return false;
            }
            extractPath= extractPath.Replace(".rar", "");
            // 创建输出目录
            if (!Directory.Exists(extractPath))
                Directory.CreateDirectory(extractPath);

            bool extractSuccess = false;
            try
            {
                // 打开RAR压缩包
                using (var archive = ArchiveFactory.OpenArchive(rarFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            // 解压文件，自动创建子文件夹
                            string fullPath = Path.Combine(extractPath, entry.Key);
                            string dir = Path.GetDirectoryName(fullPath);
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);
                            entry.WriteToFile(fullPath, new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true // 覆盖旧文件
                            });
                        }
                    }
                }
                extractSuccess = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解压失败：{ex.Message}");
                return false;
            }
        }
        static void UploadDirectory(SftpClient sftp, string localDir, string remoteDir)
        {
            // 远程目录不存在则报错
            if (!sftp.Exists(remoteDir))
            {
                MessageBox.Show($"示教器目录{remoteDir}不存在");
            }
            localDir = localDir.Replace(".rar", "");
            // 本地目录不存在则报错
            if (!Directory.Exists(localDir))
            {
                MessageBox.Show($"本地目录{localDir}不存在");
            }
         
            // 上传所有文件
            foreach (var file in Directory.GetFiles(localDir))
            {
                string fileName = Path.GetFileName(file);
                string remoteFilePath = Path.Combine(remoteDir, fileName).Replace("\\", "/");

                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    sftp.UploadFile(fs, remoteFilePath);
                }
            }

            // 递归上传子目录
            foreach (var subDir in Directory.GetDirectories(localDir))
            {
                string dirName = Path.GetFileName(subDir);
                string remoteSubDir = Path.Combine(remoteDir, dirName).Replace("\\", "/");
                UploadDirectory(sftp, subDir, remoteSubDir);
            }

            //上传完成后删除解压的压缩文件
            if (Directory.Exists(localDir))
            {
                // true：递归删除所有子目录、文件
                Directory.Delete(localDir, true);
            }
        }

    }
}
