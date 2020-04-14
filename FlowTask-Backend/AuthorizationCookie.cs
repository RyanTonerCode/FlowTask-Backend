
namespace FlowTask_Backend
{
    public struct AuthorizationCookie
    {
        public byte[] BitString { internal get; set; }

        public AuthorizationCookie(byte[] bitString)
        {
            BitString = bitString;
        }
    }
}
