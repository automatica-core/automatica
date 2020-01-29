namespace P3.Driver.HomeKitFactory
{
    public class AccessoryInstanceIdGenerator
    {
        private int _lastAid;
        private readonly object _lock = new object();

        public AccessoryInstanceIdGenerator(int lastAid)
        {
            _lastAid = lastAid;
        }

        public int GetNextAidInstance()
        {
            lock (_lock)
            {
                _lastAid += 1; // reserve 10 unique id's for every NodeInstanceFolder
                return _lastAid;
            }
        }
    }
}
