using System;

namespace Automatica.Core.Internals.Cloud.Model
{
    public interface IServerVersion
    {
        Guid ObjId { get; }
        string Version { get; }
        Version VersionObj { get; }
        string ChangeLog { get; }

        string Type { get; }
    }
}
