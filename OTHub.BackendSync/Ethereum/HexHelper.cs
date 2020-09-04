using System.Text;

namespace OTHub.BackendSync.Ethereum
{
    public static class HexHelper
    {
        public static string ByteArrayToString(byte[] ba, bool append0x = true)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return (append0x ? "0x" : "") + hex.ToString();
        }
    }
}