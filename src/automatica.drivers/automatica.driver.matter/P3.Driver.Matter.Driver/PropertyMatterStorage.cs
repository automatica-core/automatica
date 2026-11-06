using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.Matter.Storage;

namespace P3.Driver.Matter.Driver
{
    /// <summary>Keeps the Matter node state (fabrics, keys, endpoint map) in a hidden property of the bridge node.</summary>
    internal sealed class PropertyMatterStorage : IMatterStorage
    {
        private readonly IDriverContext _context;
        private readonly PropertyInstance _property;

        public PropertyMatterStorage(IDriverContext context, PropertyInstance property)
        {
            _context = context;
            _property = property;
        }

        public Task<string> LoadAsync(CancellationToken token = default)
        {
            return Task.FromResult(_property?.ValueString);
        }

        public Task SaveAsync(string json, CancellationToken token = default)
        {
            if (_property == null || _property.ValueString == json)
            {
                return Task.CompletedTask;
            }

            _property.Value = json;
            _context.NodeTemplateFactory.SetPropertyValue(_property.ObjId, json);
            return Task.CompletedTask;
        }
    }
}
