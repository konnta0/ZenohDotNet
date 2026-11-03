using System;
using System.Buffers;
using System.Text;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Provides buffer pooling utilities for reducing allocations in hot paths.
    /// </summary>
    internal static class BufferPool
    {
        private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

        /// <summary>
        /// Rents a buffer from the pool.
        /// </summary>
        /// <param name="minimumLength">The minimum required length.</param>
        /// <returns>A rented buffer (may be larger than requested).</returns>
        public static byte[] Rent(int minimumLength)
        {
            return Pool.Rent(minimumLength);
        }

        /// <summary>
        /// Returns a buffer to the pool.
        /// </summary>
        /// <param name="buffer">The buffer to return.</param>
        /// <param name="clearArray">Whether to clear the buffer before returning.</param>
        public static void Return(byte[] buffer, bool clearArray = false)
        {
            if (buffer != null)
            {
                Pool.Return(buffer, clearArray);
            }
        }
    }

    /// <summary>
    /// Provides optimized UTF-8 encoding utilities.
    /// </summary>
    internal static class Utf8Helper
    {
        private static readonly Encoding Utf8 = Encoding.UTF8;

        /// <summary>
        /// Gets the byte count for a string when encoded as null-terminated UTF-8.
        /// </summary>
        public static int GetNullTerminatedByteCount(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 1; // Just the null terminator
            return Utf8.GetByteCount(value) + 1;
        }

        /// <summary>
        /// Encodes a string to a pooled buffer as null-terminated UTF-8.
        /// Returns the actual bytes written (including null terminator).
        /// Caller must return the buffer to the pool.
        /// </summary>
        public static (byte[] Buffer, int Length) EncodeToPooledBuffer(string value)
        {
            int byteCount = GetNullTerminatedByteCount(value);
            byte[] buffer = BufferPool.Rent(byteCount);
            
            if (string.IsNullOrEmpty(value))
            {
                buffer[0] = 0;
                return (buffer, 1);
            }

            int written = Utf8.GetBytes(value, 0, value.Length, buffer, 0);
            buffer[written] = 0; // Null terminator
            return (buffer, written + 1);
        }

        /// <summary>
        /// Encodes a string directly into a provided span.
        /// Returns the number of bytes written (excluding null terminator if not requested).
        /// </summary>
        public static int EncodeToSpan(string value, Span<byte> destination, bool nullTerminate = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (nullTerminate && destination.Length > 0)
                {
                    destination[0] = 0;
                    return 1;
                }
                return 0;
            }

            int written = Utf8.GetBytes(value.AsSpan(), destination);
            if (nullTerminate)
            {
                destination[written] = 0;
                written++;
            }
            return written;
        }
    }

    /// <summary>
    /// A disposable wrapper for a rented buffer that returns it to the pool on dispose.
    /// </summary>
    internal readonly struct RentedBuffer : IDisposable
    {
        public readonly byte[] Array;
        public readonly int Length;

        public RentedBuffer(byte[] array, int length)
        {
            Array = array;
            Length = length;
        }

        public Span<byte> Span => Array.AsSpan(0, Length);

        public void Dispose()
        {
            BufferPool.Return(Array);
        }
    }
}
