using BreathAIClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace BreathAIClient.Services
{
    public class BreathAIService : IDisposable
    {
        private readonly HttpClient _http = new();
        private readonly ApiSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public BreathAIService(ApiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            var baseUrl = _settings.ApiBaseUrl?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("API Base URL 不能为空");
            if (!baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("API Base URL 必须以 /api 结尾，例如 https://chat.breathai.top/api");
            _settings.ApiBaseUrl = baseUrl; // 规范化
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            _http.Timeout = TimeSpan.FromSeconds(60);
        }

        private string ChatCompletionsUrl() => $"{_settings.ApiBaseUrl}/v1/chat/completions";

        public async Task<ChatMessage> CreateChatTextAsync(IEnumerable<ChatMessage> history, string model, double temperature = 0.7, int? maxTokens = 512, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new InvalidOperationException("未设置 API Key");

            var messagesArray = new JsonArray();
            foreach (var m in history)
            {
                var contentText = m.Content ?? string.Empty;
                messagesArray.Add(new JsonObject { ["role"] = m.Role, ["content"] = contentText });
            }

            var payload = new JsonObject
            {
                ["model"] = model,
                ["messages"] = messagesArray,
                ["temperature"] = temperature,
                ["stream"] = false
            };
            if (maxTokens.HasValue) payload["max_tokens"] = maxTokens.Value;

            var url = ChatCompletionsUrl();
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload.ToJsonString(_jsonOptions), Encoding.UTF8, "application/json")
            };

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"HTTP {(int)resp.StatusCode}: {body}");

            return ParseCompletionResponseWithUsage(body);
        }

        public async Task<ChatMessage> CreateChatVisionAsync(string userText, string dataUrlImage, string model, double temperature = 0.7, int? maxTokens = 512, CancellationToken ct = default)
        {
            var contentArray = new JsonArray
            {
                new JsonObject { ["type"] = "text", ["text"] = userText },
                new JsonObject { ["type"] = "image_url", ["image_url"] = new JsonObject { ["url"] = dataUrlImage } }
            };

            var messagesArray = new JsonArray
            {
                new JsonObject { ["role"] = "user", ["content"] = contentArray }
            };

            var payload = new JsonObject
            {
                ["model"] = model,
                ["messages"] = messagesArray,
                ["temperature"] = temperature,
                ["stream"] = false
            };
            if (maxTokens.HasValue) payload["max_tokens"] = maxTokens.Value;

            var url = ChatCompletionsUrl();
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload.ToJsonString(_jsonOptions), Encoding.UTF8, "application/json")
            };

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"HTTP {(int)resp.StatusCode}: {body}");

            return ParseCompletionResponseWithUsage(body);
        }

        private ChatMessage ParseCompletionResponseWithUsage(string body)
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var message = new ChatMessage { Role = "assistant" };

            // 解析usage信息
            if (root.TryGetProperty("usage", out var usageElem))
            {
                if (usageElem.TryGetProperty("prompt_tokens", out var promptTokensElem))
                    message.PromptTokens = promptTokensElem.GetInt32();

                if (usageElem.TryGetProperty("completion_tokens", out var completionTokensElem))
                    message.CompletionTokens = completionTokensElem.GetInt32();

                if (usageElem.TryGetProperty("total_tokens", out var totalTokensElem))
                    message.TokensUsed = totalTokensElem.GetInt32();
            }

            // 解析消息内容
            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var messageElem))
                {
                    if (messageElem.TryGetProperty("content", out var contentElem))
                    {
                        message.Content = ExtractContentFromElement(contentElem);
                    }
                }
            }

            return message;
        }

        private static string ExtractContentFromElement(JsonElement elem)
        {
            if (elem.ValueKind == JsonValueKind.String)
                return elem.GetString() ?? string.Empty;

            if (elem.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (var item in elem.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        if (item.TryGetProperty("type", out var t) && string.Equals(t.GetString(), "text", StringComparison.OrdinalIgnoreCase))
                        {
                            if (item.TryGetProperty("text", out var txt) && txt.ValueKind == JsonValueKind.String)
                                sb.Append(txt.GetString());
                        }
                        else if (item.TryGetProperty("text", out var txt2) && txt2.ValueKind == JsonValueKind.String)
                        {
                            sb.Append(txt2.GetString());
                        }
                    }
                    else if (item.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(item.GetString());
                    }
                }
                return sb.ToString();
            }
            return string.Empty;
        }

        public void Dispose() => _http.Dispose();
    }
}