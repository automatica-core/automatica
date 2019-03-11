using System;
using System.Threading.Tasks;

namespace P3.Knx.Core.Abstractions
{
    public interface IKnxDriver
    {
        void AddAddressNotifier(string address, Action<object> callback);
        Task<bool> Read(string address);
        Task<bool> Write(string address, ReadOnlyMemory<byte> data);
    }
}
