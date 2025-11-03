using BreathAIClient.ViewModels;
using ReactiveUI;
using System;

namespace BreathAIClient.Models
{
    public class ChatMessage : ViewModelBase
    {
        private string _role = "user";
        private string _content = "";
        private DateTime _timestamp = DateTime.Now;
        private bool _isStreaming = false;
        private int? _tokensUsed;
        private int? _promptTokens;
        private int? _completionTokens;

        public string Role
        {
            get => _role;
            set => this.RaiseAndSetIfChanged(ref _role, value);
        }

        public string Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => this.RaiseAndSetIfChanged(ref _timestamp, value);
        }

        public bool IsStreaming
        {
            get => _isStreaming;
            set => this.RaiseAndSetIfChanged(ref _isStreaming, value);
        }

        public int? TokensUsed
        {
            get => _tokensUsed;
            set => this.RaiseAndSetIfChanged(ref _tokensUsed, value);
        }

        public int? PromptTokens
        {
            get => _promptTokens;
            set => this.RaiseAndSetIfChanged(ref _promptTokens, value);
        }

        public int? CompletionTokens
        {
            get => _completionTokens;
            set => this.RaiseAndSetIfChanged(ref _completionTokens, value);
        }

        // 使用 ObservableAsPropertyHelper 实现计算属性
        private readonly ObservableAsPropertyHelper<string> _tokenInfo;
        public string TokenInfo => _tokenInfo.Value;

        public ChatMessage()
        {
            // 当token相关属性变化时，更新TokenInfo
            this.WhenAnyValue(
                x => x.TokensUsed,
                x => x.PromptTokens,
                x => x.CompletionTokens,
                (tokens, prompt, completion) =>
                {
                    if (!tokens.HasValue && !prompt.HasValue && !completion.HasValue)
                        return null;

                    if (prompt.HasValue && completion.HasValue)
                        return $"提示词: {prompt} | 补全: {completion} | 总计: {prompt + completion}";

                    if (tokens.HasValue)
                        return $"Tokens: {tokens}";

                    return null;
                })
                .ToProperty(this, x => x.TokenInfo, out _tokenInfo);
        }
    }
}
