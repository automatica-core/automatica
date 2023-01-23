namespace P3.Driver.Loxone.Miniserver.Driver.Data
{
    public class GetKeyValueLL
    {
        public string Key { get; set; }
        public string Salt { get; set; }
    }

    public class GetKeyResponseLL : LoxoneApiResponseLL
    {
        public GetKeyValueLL Value { get; set; }
    }
}
