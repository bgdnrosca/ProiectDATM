using System;

namespace BluetoothChat.Helpers
{
    public static class Constants
    {
        public static Guid BluetoothDatmGUID = Guid.Parse("8748aa28-6c8c-42d5-af38-e3cd785e7724");

        public const UInt16 SdpServiceNameAttributeId = 0x100;

        public const byte SdpServiceNameAttributeType = (4 << 3) | 5;

        public const string SdpServiceName = "DATM Chat Service";
    }
}