using System;

namespace Automatica.Core.Internals.Cloud.Model;

public class TextToSpeechRequest
{
    public Guid Id { get; set; }
    public string Text { get; set; }
    public string Language { get; set; }
    public string Voice { get; set; }
}