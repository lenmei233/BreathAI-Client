using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using BreathAIClient.Models;
using BreathAIClient.Services;
using BreathAIClient.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Ursa.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace BreathAIClient.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        private ApiSettings _settings;
        private BreathAIService? _service;
        private string _inputText = "";
        private string _selectedModel = "llama-3.1-8b-instant";
        private bool _busy;

        // 输入框高度相关（用于 View 绑定）
        private double _inputTextHeight = 40;
        public double InputTextHeight
        {
            get => _inputTextHeight;
            set => this.RaiseAndSetIfChanged(ref _inputTextHeight, value);
        }
        public double InputTextMinHeight { get; } = 40;
        public double InputTextMaxHeight { get; } = 200;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public string InputText
        {
            get => _inputText;
            set
            {
                this.RaiseAndSetIfChanged(ref _inputText, value);
                // 文本为空时重置高度（发送后 ViewModel 会把 InputText 设为空）
                if (string.IsNullOrEmpty(value))
                    InputTextHeight = InputTextMinHeight;
            }
        }

        public string SelectedModel
        {
            get => _selectedModel;
            set => this.RaiseAndSetIfChanged(ref _selectedModel, value);
        }

        public bool Busy
        {
            get => _busy;
            set => this.RaiseAndSetIfChanged(ref _busy, value);
        }
        private Visual? _viewContext;
        public Visual? ViewContext
        {
            get => _viewContext;
            set => this.RaiseAndSetIfChanged(ref _viewContext, value);
        }

        public List<string> AvailableModels => new()
        {
            // === GPT-OSS 系列 ===
            "gpt-oss-120b",
            "gpt-oss-120b-low",
            "gpt-oss-120b-medium",
            "gpt-oss-120b-high",
            "gpt-oss-20b",
            "gpt-oss-20b-low",
            "gpt-oss-20b-medium",
            "gpt-oss-20b-high",

            // === Llama 系列 ===
            "llama-3.1-8b-instant",
            "llama-3.3-70b-versatile",
            "llama-4-maverick",
            "llama-4-scout",
            "llama-3.1-8b-instant",
    
            // === Llama 最快速系列 ===
            "llama-3.3-70b-maxspeed",
            "llama3.1-8b-maxspeed",
            "qwen-3-32b-maxspeed",
            "qwen-3-coder-480b-maxspeed",
            "qwen-3-235b-a22b-instruct-2507-maxspeed",

            // === DeepSeek 系列 ===
            "deepseek-v3.1-terminus",
            "deepseek-v3.2-exp",
            "deepseek-r1",
            "deepseek-v3",
            "deepseek-ocr",
            "deepseek-r1-0528-qwen3-8b",
    
            // === Qwen3 常规模型 ===
            "qwen3-32b-ultrafast",
            "qwen3-30b-a3b",
            "qwen3-32b",
            "qwen3-235b-a22b",
    
            // === Qwen3 思考器模式 ===
            "qwen3-coder-30b-a3b-instruct",
            "qwen3-coder-480b-a35b-instruct",
            "qwen3-30b-a3b-thinking-2507",
            "qwen3-30b-a3b-instruct-2507",
            "qwen3-235b-a22b-thinking-2507",
            "qwen3-235b-a22b-instruct-2507",
            "qwenlong-l1-32b",
    
            // === Qwen3 多模态（图片视觉输入）===  
            "qwen3-vl-32b-instruct",
            "qwen3-vl-8b-instruct",
            "qwen3-vl-30b-a3b-instruct",
            "qwen3-vl-235b-a22b-instruct",
    
            // === Qwen3 思考模式 + 图像模式 ===
            "qwen3-vl-32b-thinking",
            "qwen3-vl-8b-thinking",
            "qwen3-vl-30b-a3b-thinking",
            "qwen3-vl-235b-a22b-thinking",
    
            // === Qwen3 Omni 多模态（Advanced） ===
            "qwen3-omni-30b-a3b-instruct",
            "qwen3-omni-30b-a3b-thinking",
            "qwen3-omni-30b-a3b-captioner",

            // === Kimi 系列 ===
            "kimi-k2-instruct-0905",

            // === Grok 系列（Grok3 / Grok4）=== 
            "grok-3-mini",
            "grok-3-mini-nsfw",
            "grok-4-fast-non-reasoning",
            "grok-4-fast-non-reasoning-nsfw",
            "grok-4-fast-reasoning",
            "grok-4-fast-reasoning-nsfw",

            // === GLM4 系列（智谱AI）====
            "glm-4.6",
            "glm-4.5v",
            "glm-4.5",
            "glm-4.5-air",
            "glm-4.1v-9b-thinking",
            "glm-4.1v-9b-thinking-pro",

            // === Ring系列 ===
            "ring-1t",
            "ring-flash-2.0",

            // === Ling系列 ===
            "ling-1t",
            "ling-flash-2.0",
            "ling-mini-2.0",

            // === Step系列 ===
            "step3",

            // === Compound模型（Baichuan 等）==== 
            "compound",
            "compound-mini",

            // === HunYuan 等 ===
            "hunyuan-mt-7b",

            // === 其他 ===
            "breath"
        };
        public ReactiveCommand<ChatMessage, Unit> CopyMessageCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearChatCommand { get; }
        public ReactiveCommand<Unit, Unit> SendCommand { get; }
        public ReactiveCommand<Window?, Unit> SendWithImageCommand { get; }
        public ChatViewModel()
        {
            var canSend = this.WhenAnyValue(x => x.InputText, x => x.Busy, (txt, busy) => !busy && !string.IsNullOrWhiteSpace(txt));
            SendCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_service == null) return;
                var text = InputText?.Trim();
                if (string.IsNullOrEmpty(text)) return;
                Busy = true;
                Messages.Add(new ChatMessage { Role = "user", Content = text });

                try
                {
                    var reply = await _service.CreateChatTextAsync(Messages, SelectedModel, 0.7, 512);
                    Messages.Add(new ChatMessage { Role = "assistant", Content = reply.Content });
                    InputText = "";
                }
                catch (System.Exception ex)
                {
                    Messages.Add(new ChatMessage { Role = "assistant", Content = $"错误：{ex.Message}" });
                }
                finally
                {
                    Busy = false;
                }
            }, canSend);

            var canSendImg = this.WhenAnyValue(x => x.Busy, busy => !busy);
            SendWithImageCommand = ReactiveCommand.CreateFromTask<Window?>(async owner =>
            {
                if (_service == null) return;

                var text = InputText?.Trim();
                if (string.IsNullOrEmpty(text)) text = "请识别这张图片";

                var ofd = new OpenFileDialog
                {
                    AllowMultiple = false,
                    Filters = new()
                    {
                        new FileDialogFilter { Name = "Image", Extensions = { "png", "jpg", "jpeg", "webp" } }
                    }
                };

                var files = await ofd.ShowAsync(owner);

                if (files == null || files.Length == 0) return;

                var path = files[0];
                var bytes = await File.ReadAllBytesAsync(path);

                var mime = Path.GetExtension(path).ToLowerInvariant() switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                var dataUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";

                Busy = true;
                try
                {
                    Messages.Add(new ChatMessage { Role = "user", Content = $"[图片] {Path.GetFileName(path)}\n{text}" });
                    var reply = await _service.CreateChatVisionAsync(text, dataUrl, SelectedModel, 0.7, 512);
                    Messages.Add(new ChatMessage { Role = "assistant", Content = reply.Content });
                    InputText = "";
                }
                catch (Exception ex)
                {
                    Messages.Add(new ChatMessage { Role = "assistant", Content = $"错误：{ex.Message}" });
                }
                finally
                {
                    Busy = false;
                }
            }, canSendImg);
            ClearChatCommand = ReactiveCommand.Create(() => Messages.Clear());
            CopyMessageCommand = ReactiveCommand.Create<ChatMessage>(async msg =>
            {
                if (msg == null || string.IsNullOrEmpty(msg.Content)) return;
                try
                {
                    var clipboard = GetClipboard();
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(msg.Content);
                        await MessageBox.ShowAsync($"已复制到剪贴板","提示", MessageBoxIcon.Success);
                    }
                    else
                    {
                        await MessageBox.ShowAsync("无法获取剪贴板服务","错误",MessageBoxIcon.Error);
                    }
                }
                catch(Exception ex)
                {
                    await MessageBox.ShowAsync($"复制失败{ex.Message}","错误",MessageBoxIcon.Error);
                }
            });
        }

        private IClipboard? GetClipboard()
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(_viewContext);
                return topLevel?.Clipboard;
            }
            catch
            {
                return null;
            }
        }

        public async Task InitializeAsync()
        {
            _settings = await SettingsStorage.LoadAsync();
            SelectedModel = _settings.DefaultModel;
            _service = new BreathAIService(_settings);
        }
    }
}