using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Configuration;

namespace Automatica.Core.EF.Backup
{
    public interface IDatabaseBackup
    {
        DatabaseTypeEnum DbType { get; }

        Task StartBackup(string connectionString, string targetFile, CancellationToken token = default);
    }
}
