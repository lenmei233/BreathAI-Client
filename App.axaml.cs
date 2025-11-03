using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BreathAIClient.ViewModels;
using BreathAIClient.Views;
using System;
using System.Threading.Tasks;

namespace BreathAIClient
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var settingsVm = new SettingsViewModel();
                var chatVm = new ChatViewModel();
                var aboutVm = new AboutViewModel();
                // 先创建并显示窗口，避免初始化错误阻止窗口呈现
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(chatVm, settingsVm,aboutVm)
                };

                // 异步初始化 VMs（不阻塞 UI），并妥善捕获异常后回写到 UI
                _ = InitializeViewModelsAsync(settingsVm, chatVm);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static async Task InitializeViewModelsAsync(SettingsViewModel settingsVm, ChatViewModel chatVm)
        {
            try
            {
                await settingsVm.LoadAsync();
            }
            catch (Exception ex)
            {
                // 在 UI 线程上设置状态消息
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    settingsVm.StatusMessage = $"加载设置失败：{ex.Message}";
                });
            }

            try
            {
                await chatVm.InitializeAsync();
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    chatVm.Messages.Add(new BreathAIClient.Models.ChatMessage
                    {
                        Role = "assistant",
                        Content = $"初始化聊天服务失败：{ex.Message}"
                    });
                });
            }
        }
    }
}