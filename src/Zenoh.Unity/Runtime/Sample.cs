using System;
using System.Text;

namespace Zenoh.Unity
{
    /// <summary>
    /// Represents a sample received from a Zenoh subscriber in Unity.
    /// </summary>
    public class Sample
    {
        /// <summary>
        /// Gets the key expression of this sample.
        /// </summary>
        public string KeyExpression { get; }

        /// <summary>
        /// Gets the raw payload data.
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// Initializes a new instance of the Sample class.
        /// </summary>
        /// <param name="keyExpression">The key expression.</param>
        /// <param name="payload">The payload data.</param>
        public Sample(string keyExpression, byte[] payload)
        {
            KeyExpression = keyExpression ?? throw new ArgumentNullException(nameof(keyExpression));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        /// <summary>
        /// Gets the payload as a UTF-8 encoded string.
        /// </summary>
        /// <returns>The payload as a string.</returns>
        public string GetPayloadAsString()
        {
            return Encoding.UTF8.GetString(Payload);
        }

        /// <summary>
        /// Tries to get the payload as a UTF-8 string.
        /// </summary>
        /// <param name="result">The decoded string if successful.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool TryGetPayloadAsString(out string result)
        {
            try
            {
                result = GetPayloadAsString();
                return true;
            }
            catch
            {
                result = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Returns a string representation of this sample.
        /// </summary>
        public override string ToString()
        {
            return $"Sample[KeyExpr={KeyExpression}, PayloadLen={Payload.Length}]";
        }
    }
}
