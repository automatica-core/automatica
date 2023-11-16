using System;

namespace Automatica.Core.Internals.Cloud.Model;

public class TextToSpeechResponse
{
    public string Url { get; set; }
    public TimeSpan AudioDuration { get; set; }
}