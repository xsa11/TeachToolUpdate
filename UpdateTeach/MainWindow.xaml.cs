using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Renci.SshNet;

namespace UpdateTeach
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UpdViewModel updViewModel = new UpdViewModel();
        UpdFTP updFTP = new UpdFTP();
        public MainWindow()
        {
            InitializeComponent();
            var localIpList = updViewModel.GetAllLocalIpV4();
            foreach (var ip in localIpList)
            {
                Application.Current.Dispatcher.Invoke(() => { comb_local.Items.Add(ip); });

            }
           
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            btn_Search.IsEnabled = false;
            btn_Restart.IsEnabled = false;
            btn_Update.IsEnabled = false;
            string host = txt_Host.Text;
            int port = Convert.ToInt32(txt_Port.Text); // 默认是22，如果不是则需修改
            string username = txt_Username.Text;
            string password = txt_Password.Password.ToString();
            string remotePath = txt_RemotePath.Text; // 远程目录路径
            string localPath = txt_LocalPath.Text; // 本地目录路径
            ProgressBar.Value = 0;
            updFTP.progress = 0;
            ErrorInfo.Text = "";
           var progress= Task.Run(async() =>
            {
                // 这里放置需要异步执行的代码
                while (updFTP.progress < 100)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = updFTP.progress; // 更新进度条的值

                    });
                  
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value =100; // 更新进度条的值

                });
            });
            if (updFTP.Unrar(localPath, localPath))
            {
                ErrorInfo.Text = await updFTP.ConnectFTP(host, port, username, password, remotePath, localPath,rad_Backup.IsChecked.Value);
                if (ErrorInfo.Text.ToString().Contains("更新示教器成功"))
                {
                    ErrorInfo.Text = await updViewModel.ConnectSSH(host, port, username, password);
                  
                }
            }
            btn_Search.IsEnabled = true;
            btn_Restart.IsEnabled = true;
            btn_Update.IsEnabled = true;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string host = txt_Host.Text;
            int port = Convert.ToInt32(txt_Port.Text); // 默认是22，如果不是则需修改
            string username = txt_Username.Text;
            string password = txt_Password.Password.ToString();
            updViewModel.ConnectSSH(host, port, username, password);

        }

        private async void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            string networkAddress = txt_Search.Text; // 你的局域网地址

            // 扫描 192.168.0.1 ~ 192.168.0.254
            var onlineIpList = await updViewModel.ScanAsync(networkAddress);
            var res = MessageBox.Show($"是否搜索网段{txt_Search.Text}?", "", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    comb_Res.Items.Clear();
                });
                    foreach (var ip in onlineIpList)
                {
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                      
                        comb_Res.Items.Add(ip); 
                    });

                }
            }


        }

        private void comb_local_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var susbend = comb_local.SelectedValue.ToString().Split('.');
            txt_Search.Text = susbend[0] + "." + susbend[1] + "." + susbend[2] + ".";
        }

        private void btn_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // 文件选择对话框
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "RAR压缩包|*.rar",
                Title = "请选择要解压的RAR文件"
            };

            if (dialog.ShowDialog() != true)
                return;

            txt_LocalPath.Text = dialog.FileName;
        }

        private void rad_OpenFile_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}