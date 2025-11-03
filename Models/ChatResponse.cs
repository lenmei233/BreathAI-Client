
namespace BreathAIClient.Models
{
    public class ChatResponse
    {
        public string Text { get; set; } = "";
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
