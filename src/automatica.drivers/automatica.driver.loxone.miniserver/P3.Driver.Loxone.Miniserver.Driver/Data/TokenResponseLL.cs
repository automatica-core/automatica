namespace P3.Driver.Loxone.Miniserver.Driver.Data
{
    public class TokenData
    {
        public string Token { get; set; }

        public string Key { get; set; }
        public int ValidUntil { get; set; }
        public int TokenRights { get; set; }
        public bool UnsecurePass { get; set; }
    }
    public class TokenResponseLL : LoxoneApiResponseLL
    {
        public TokenData Value { get; set; }
    }
}
