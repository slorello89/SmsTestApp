using Newtonsoft.Json;

namespace Nexmo
{
    public class SmsRequestObject
    {
        [JsonProperty("to")]
        public To To { get; set; }

        [JsonProperty("from")]
        public From From { get; set; }

        [JsonProperty("message")]
        public Message Message { get; set; }

        public SmsRequestObject(string to, string from, string message)
        {
            this.To = new To(to);
            this.From = new From(from);
            this.Message = new Message(message);
        }
    }

    public class To
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        public To(string number)
        {
            this.Type = "sms";
            this.Number = number;
        }
    }

    public class From
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        public From(string number)
        {
            this.Type = "sms";
            this.Number = number;
        }
    }    

    public class Message
    {
        [JsonProperty("content")]
        public Content Content { get; set; }

        public Message(string text)
        {
            Content = new Content(text);
        }
    }

    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        public Content(string text)
        {
            Type = "text";
            this.Text = text;
        }
    }
}
