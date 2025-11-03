using BreathAIClient.Models;
using BreathAIClient.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace BreathAIClient.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _apiBaseUrl = "https://chat.breathai.top/api";
        private string _apiKey = "";
        private string _defaultModel = "llama-3.1-8b-instant";
        private string _statusMessage = "";

        public string ApiBaseUrl { get => _apiBaseUrl; set => this.RaiseAndSetIfChanged(ref _apiBaseUrl, value); }
        public string ApiKey { get => _apiKey; set => this.RaiseAndSetIfChanged(ref _apiKey, value); }
        public string DefaultModel { get => _defaultModel; set => this.RaiseAndSetIfChanged(ref _defaultModel, value); }
        public string StatusMessage { get => _statusMessage; set => this.RaiseAndSetIfChanged(ref _statusMessage, value); }

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> TestCommand { get; }

        public SettingsViewModel()
        {
            SaveCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var s = new ApiSettings
                {
                    ApiBaseUrl = ApiBaseUrl?.TrimEnd('/'),
                    ApiKey = ApiKey?.Trim(),
                    DefaultModel = DefaultModel?.Trim()
                };
                await SettingsStorage.SaveAsync(s);
                StatusMessage = "设置已保存";
            });

            TestCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                StatusMessage = "测试连接中...";
                var s = new ApiSettings
                {
                    ApiBaseUrl = ApiBaseUrl?.TrimEnd('/'),
                    ApiKey = ApiKey?.Trim(),
                    DefaultModel = DefaultModel?.Trim()
                };

                try
                {
                    using var svc = new BreathAIService(s);
                    var messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "user", Content = "ping" }
                    };

                    var result = await svc.CreateChatTextAsync(messages, s.DefaultModel, 0.2, 64);

                    var content = result?.Content ?? "无响应";
                    StatusMessage = $"连接成功：{(content.Length > 40 ? content[..40] + "..." : content)}";
                }
                catch (System.Exception ex)
                {
                    StatusMessage = $"连接失败：{ex.Message}";
                }
            });
        }

        public async Task LoadAsync()
        {
            var s = await SettingsStorage.LoadAsync();
            ApiBaseUrl = s.ApiBaseUrl;
            ApiKey = s.ApiKey;
            DefaultModel = s.DefaultModel;
        }
    }
}