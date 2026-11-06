namespace P3.Driver.HomeKit.Hap.TlvData
{
    public enum Constants
    {
        Method = 0,
        Identifier = 1,
        Salt = 2,
        PublicKey = 3,
        Proof = 4,
        EncryptedData = 5,
        State = 6,
        Error = 7,
        Signature = 10,
        Permissions = 11,
        Separator = 255
    }

    public enum ErrorCodes
    {
        Unknown = 1,
        Authentication = 2,
        Backoff = 3,
        MaxPeers = 4,
        MaxTries = 5,
        Unavailable = 6,
        Busy = 7
    }
}
