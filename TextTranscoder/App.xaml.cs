using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;

namespace TextTranscoder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //注册全局异常事件
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex && Dispatcher != null)
                {
                    HandyControl.Controls.MessageBox.Error(ex.ToString(), "主线程未捕获的异常");
                }
            };
            DispatcherUnhandledException += (sender, args) =>
            {
                HandyControl.Controls.MessageBox.Error(args.Exception.ToString(), "子线程未捕获的异常");
                args.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                HandyControl.Controls.MessageBox.Error(args.Exception.ToString(), "未发现的异常");
                args.SetObserved();
            };

            // 注册编码提供程序
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }

}
