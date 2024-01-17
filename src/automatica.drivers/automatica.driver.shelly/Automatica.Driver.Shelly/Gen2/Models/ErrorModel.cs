using System;
using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Gen2.Models
{
    internal class ErrorModel
    {
        private int _code;
        private string _message;

        [JsonProperty("code")]
        public int Code
        {
            get => _code;
            set { _code = value;
                ParseMessage();
            }
        }

        [JsonProperty("message")]
        public string Message
        {
            get => _message;
            set { _message = value;
                ParseMessage();
            }
        }

        public object MessageObject { get; set; }

        private void ParseMessage()
        {
            if (!String.IsNullOrEmpty(Message))
            {
                if (Code == 401)
                {
                    MessageObject = JsonConvert.DeserializeObject<AuthModel>(Message);
                }
            }
        }
    }
}
