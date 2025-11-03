using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreathAIClient.Models
{
    public class ApiSettings
    {
        public string ApiBaseUrl { get; set; } = "https://chat.breathai.top/api";
        public string ApiKey { get; set; } = string.Empty;
        public string DefaultModel { get; set; } = "llama-3.1-8b-instant";
    }
}
