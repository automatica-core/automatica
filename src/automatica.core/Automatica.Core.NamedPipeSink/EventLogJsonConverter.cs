using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.NamedPipe
{

    public class EventLogFactory
    {
        public DateTimeOffset TimeStamp;
        public int Level;
        public Exception Exception;
        public MessageTemplateFactory MessageTemplate;
        public KeyValuePair<string, LogEventPropertyValue>[] Properties;

        public class MessageTemplateFactory
        {
            public string Text;
            public Parsing.MessageTemplateToken[] Tokens;
        }


        public LogEvent ToLogEvent()
        {
            var props = Properties.Select(x => new LogEventProperty(x.Key, x.Value));
                        
            return new LogEvent(TimeStamp, (LogEventLevel)Level, Exception, new MessageTemplate(MessageTemplate.Text, MessageTemplate.Tokens), props);
        }

    }


    public class LogEventJsonConverter : JsonConverter
    {




        public override bool CanRead => true;
        public override bool CanConvert(Type objectType) => objectType == typeof(LogEvent);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var o = JObject.Load(reader);
            try
            {
                var timeStamp = o.Value<DateTime>("Timestamp");
                var level = o.Value<int>("Level");
                var exception = o.Value<Exception>("Exception");

                var properties = CreateEventProperties(o.SelectToken("Properties"));
                var template = CreateMessageTemplate(o.SelectToken("MessageTemplate"), properties);
                

                LogEvent e = new LogEvent(timeStamp, (LogEventLevel)level, exception, template, properties);
                return e;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "PreviewDetailConverter failed to deserialize object.");
                return null;
            }

        }

        MessageTemplate CreateMessageTemplate(JToken o, List<LogEventProperty> properties)
        {
            var tokens = new List<PropertyToken>();
            string txt = o.Value<string>("Text");
            foreach (var t in o["Tokens"].ToArray())
            {
                if (o.SelectToken("Text") != null)
                {
                    new TextToken(o.Value<string>("Text"));
                }
                else
                {
                    var n = o.Value<string>("PropertyName");
                    var l = o.Value<int>("Length");
                    var d = o.Value<int>("Destructuring");
                    var f = o.Value<string>("Format");
                    // var a = o.Value<int>("Alignment");
                    var s = o.Value<int>("StartIndex");
                    var pt = new PropertyToken(n, "", f, null, (Destructuring)d);
                    tokens.Add(pt);
                }

            }
            
            return new MessageTemplate(txt, tokens);
        }

        List<LogEventProperty> CreateEventProperties(JToken o)
        {
            var results = new List<LogEventProperty>();
            
            foreach (var i in o.SelectTokens("Properties"))
            {
                var key = i.First().ToString();
                var value = i.Last()["Value"];

                //new LogEventProperty(key, new LogEventPropertyValue)
            }

            return results;
        }
    }
}
