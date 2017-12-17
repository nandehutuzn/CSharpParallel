using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chapter9_Async
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] _fileNames = {
            "AzureMobileServicesSdk.cab",
             "AzureMobileServicesSdk.msi",
              "AzureServices.cab",
               "AzureServices.chs.cab",
                "AzureServices.chs.msi",
                 "AzureServices.msi",
                  "AzureTools.Notifications.msi",
                   "CommonAzureTools.cab",
                    "CommonAzureTools.chs.cab",
                     "CommonAzureTools.chs.msi",
                      "CommonAzureTools.msi",
        };

        private List<Task<string>> _fileTasks;
        private const int BUFFER_SIZE = 0x2000;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConcatTextFiles_Click(object sender, RoutedEventArgs e)
        {
            _fileTasks = new List<Task<string>>(_fileNames.Length);

            foreach (string fileName in _fileNames)
            {
                var readTask = ReadAllTextAsync(fileName);
                _fileTasks.Add(readTask);
            }

            Task.Factory.ContinueWhenAll(_fileTasks.ToArray(), antecentTasks =>
            {
                Task.WaitAll(_fileTasks.ToArray());
                var sb = new StringBuilder();
                foreach (Task<string> fileTask in _fileTasks)
                {
                    sb.Append(fileTask.Result);
                }
                Console.WriteLine(sb);
            });

        }

        private Task<string> ReadAllTextAsync(string path)
        {
            path = System.IO.Path.Combine(@"D:\backup\VS2013_RTM_ULT_CHS\packages\CT", path);
            FileInfo info = new FileInfo(path);
            if (!info.Exists)
            {
                throw new FileNotFoundException($"{path} does not exist.");
            }

            byte[] data = new byte[info.Length];

            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read
                , FileShare.Read, BUFFER_SIZE, true);

            Task<int> task = Task<int>.Factory.FromAsync(stream.BeginRead,
                stream.EndRead, data, 0, data.Length, null, TaskCreationOptions.None);

            return task.ContinueWith(t =>
            {
                stream.Close();
                Console.WriteLine($"One task has rean {t.Result} bytes from {stream.Name}");

                return t.Result > 0 ? new UTF8Encoding().GetString(data) : "";
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
