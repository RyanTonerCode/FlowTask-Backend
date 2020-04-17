
namespace FlowTask_Backend
{

    /// <summary>
    /// This struct encapsulates an authorization cookie that ensures the user has logged in to the system.
    /// </summary>
    public struct AuthorizationCookie
    {
        private readonly byte[] bitString;

        public byte[] GetBitString()
        {
            return bitString;
        }

        /// <summary>
        /// Assign the bit string value
        /// </summary>
        /// <param name="BitString"></param>
        public AuthorizationCookie(byte[] BitString)
        {
            bitString = BitString;
        }

        /// <summary>
        /// Does not implement the equals method.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return false;
        }

        public override int GetHashCode()
        {
            throw new System.NotImplementedException();
        }

        public static bool operator ==(AuthorizationCookie left, AuthorizationCookie right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AuthorizationCookie left, AuthorizationCookie right)
        {
            return !(left == right);
        }
    }
}
