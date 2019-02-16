using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.HomeKit.Hap.Data
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
        Permissions = 11
    }

    public enum ErrorCodes
    {
        Authentication = 2,
        MaxPeers = 4,
        Busy = 7
    }
}
