using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace P3.Knx.Core.Ets
{
    public class EtsGroup : EtsEntity
    {
        public EtsGroup Parent { get; }
        private readonly List<EtsEntity> _children = new List<EtsEntity>();
        public List<EtsEntity> Children { get { return _children; } }
        public IEnumerable<EtsGroup> SubGroups { get {  return Children.Where(c => c is EtsGroup).Cast<EtsGroup>(); } }
        public IEnumerable<EtsDatapoint> Datapoints { get { return Children.Where(c => c is EtsDatapoint).Cast<EtsDatapoint>(); } }

        public int Level { get; private set; }

        public EtsGroup(XElement e, EtsGroup parent)
            : base(e)
        {
            Parent = parent;
            Level = parent != null ? parent.Level + 1 : 0;
            int rangeStart;
            if (int.TryParse(GetAttributeValue(e, "RangeStart"), out rangeStart))
            {
                if (Level == 0)
                    GroupIndex = rangeStart / 2048;
                else
                    GroupIndex = (rangeStart >> 8) % 8;
            }
            else
            {
                GroupIndex = -1;
            }
        }
        public void AddChild(EtsEntity e)
        {
            int index = 0;
            while (index < _children.Count && _children[index].GroupIndex < e.GroupIndex)
                index++;
            _children.Insert(index, e);
        }
    }
}
