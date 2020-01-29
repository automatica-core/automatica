using System;
using System.Collections.Generic;
using System.Linq;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.Hap
{
    public class AccessoryContainer
    {
        internal AccessoryContainer()
        {
            AccessoryMap = new Dictionary<int, Accessory>();
        }

        public List<Accessory> Accessories => AccessoryMap.Values.ToList();

        public void AddAccessory(Accessory accessory)
        {
            if (AccessoryMap.ContainsKey(accessory.Id))
            {
                throw new ArgumentException($"Accessory with id {accessory.Id} already existing!");
            }

            AccessoryMap.Add(accessory.Id, accessory);
        }

        public Dictionary<int, Accessory> AccessoryMap { get; set; }
    }
}
