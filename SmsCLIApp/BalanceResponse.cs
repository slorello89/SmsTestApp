using Newtonsoft.Json;

namespace Nexmo
{
    public class BalanceResponse
    {
        [JsonProperty("value")]
        public double Balance { get; set; }

        [JsonProperty("autoReload")]
        public bool AutoReload { get; set; }
    }
}
