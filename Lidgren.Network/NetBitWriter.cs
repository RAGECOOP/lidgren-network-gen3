//#define UNSAFE
//#define BIGENDIAN
/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.

*/
using System;

using System.Diagnostics;

namespace Lidgren.Network
{
    /// <summary>
    /// Helper class for NetBuffer to write/read bits
    /// </summary>
    public static class NetBitWriter
    {
        /// <summary>
        /// Read 1-8 bits from a buffer into a byte
        /// </summary>
        public static byte ReadByte(byte[] fromBuffer, int numberOfBits, int readBitOffset)
        {
            Assert(((numberOfBits > 0) && (numberOfBits < 9)), "Read() can only read between 1 and 8 bits");

            int bytePtr = readBitOffset >> 3;
            int startReadAtIndex = readBitOffset - (bytePtr * 8); // (readBitOffset % 8);

            if (startReadAtIndex == 0 && numberOfBits == 8)
                return fromBuffer[bytePtr];

            // mask away unused bits lower than (right of) relevant bits in first byte
            byte returnValue = (byte)(fromBuffer[bytePtr] >> startReadAtIndex);

            int numberOfBitsInSecondByte = numberOfBits - (8 - startReadAtIndex);

            if (numberOfBitsInSecondByte < 1)
            {
                // we don't need to read from the second byte, but we DO need
                // to mask away unused bits higher than (left of) relevant bits
                return (byte)(returnValue & (255 >> (8 - numberOfBits)));
            }

            byte second = fromBuffer[bytePtr + 1];

            // mask away unused bits higher than (left of) relevant bits in second byte
            second &= (byte)(255 >> (8 - numberOfBitsInSecondByte));

            return (byte)(returnValue | (byte)(second << (numberOfBits - numberOfBitsInSecondByte)));
        }

        /// <summary>
        /// Read several bytes from a buffer
        /// </summary>
        public static void ReadBytes(byte[] fromBuffer, int numberOfBytes, int readBitOffset, byte[] destination, int destinationByteOffset)
        {
#if UNSAFE
            unsafe
            {
                var readByteOffset = readBitOffset >> 3;
                readBitOffset -= readByteOffset * 8;
                AssertAlways(fromBuffer.Length >= (readBitOffset == 0 ? readByteOffset : (readByteOffset + 1)));
                fixed (byte* pFrom = &fromBuffer[readByteOffset], pDest = destination)
                {
                    ReadBytes(pFrom, numberOfBytes, readBitOffset, pDest);
                }
            }
#else
            int readPtr = readBitOffset >> 3;
            int segReadOffset = readBitOffset - (readPtr * 8); // (readBitOffset % 8);

            if (segReadOffset == 0)
            {
                Buffer.BlockCopy(fromBuffer, readPtr, destination, destinationByteOffset, numberOfBytes);
                return;
            }

            int firstPartLen = 8 - segReadOffset;

            for (int i = 0; i < numberOfBytes; i++)
            {
                destination[destinationByteOffset++] = (byte)
                    (fromBuffer[readPtr] >> segReadOffset // Shift out lower bits in first byte
                    | (fromBuffer[++readPtr] << firstPartLen)); // Shift out higher bits in second byte
            }
#endif
        }

#if UNSAFE
        /// <summary>
        /// Read an sequence of bytes from the buffer
        /// </summary>
        [CLSCompliant(false)]
        public static unsafe void ReadBytes(byte* fromBuffer, int cbToCopy, int readBitOffset, byte* destination)
        {
            Assert(0 <= readBitOffset && readBitOffset < 8);
            if (readBitOffset == 0)
            {
                Buffer.MemoryCopy(fromBuffer, destination, cbToCopy, cbToCopy);
                return;
            }

            int firstPartLen = 8 - readBitOffset;
            for (int i = 0; i < cbToCopy; i++)
            {
                destination[i] = (byte)
                    (fromBuffer[i] >> readBitOffset // Shift out lower bits in first byte
                    | (fromBuffer[i + 1] << firstPartLen)); // Shift out higher bits in second byte
            }
        }

        /// <summary>
        /// Read a POCO struct from buffer
        /// </summary>
        [CLSCompliant(false)]
        public static unsafe void ReadStruct<T>(out T value, byte* fromBuffer, int readBitOffset) where T : unmanaged
        {
            if (readBitOffset == 0)
                value = *(T*)fromBuffer;
            else
                fixed (T* pDest = &value)
                {
                    ReadBytes(fromBuffer, sizeof(T), readBitOffset, (byte*)pDest);
                }
        }

        public static unsafe void ReadStruct<T>(out T value, byte[] fromBuffer, int readBitOffset) where T : unmanaged
        {
            fixed (byte* pSrc = fromBuffer)
            {
                var readByteOffset = readBitOffset >> 3;
                readBitOffset -= readByteOffset * 8;
                AssertAlways(fromBuffer.Length >= (readBitOffset == 0 ? readByteOffset : (readByteOffset + 1)));
                ReadStruct(out value, pSrc + readByteOffset, readBitOffset);
            }
        }
#endif

        /// <summary>
        /// Write 0-8 bits of data to buffer
        /// </summary>
        public static void WriteByte(byte source, int numberOfBits, byte[] destination, int destBitOffset)
        {
            if (numberOfBits == 0)
                return;

            Assert(((numberOfBits >= 0) && (numberOfBits <= 8)), "Must write between 0 and 8 bits!");

            // Mask out all the bits we dont want
            source = (byte)(source & (0xFF >> (8 - numberOfBits)));

            int p = destBitOffset >> 3;
            int bitsUsed = destBitOffset & 0x7; // mod 8
            int bitsFree = 8 - bitsUsed;
            int bitsLeft = bitsFree - numberOfBits;

            // Fast path, everything fits in the first byte
            if (bitsLeft >= 0)
            {
                int mask = (0xFF >> bitsFree) | (0xFF << (8 - bitsLeft));

                destination[p] = (byte)(
                    // Mask out lower and upper bits
                    (destination[p] & mask) |

                    // Insert new bits
                    (source << bitsUsed)
                );

                return;
            }

            destination[p] = (byte)(
                // Mask out upper bits
                (destination[p] & (0xFF >> bitsFree)) |

                // Write the lower bits to the upper bits in the first byte
                (source << bitsUsed)
            );

            p += 1;

            destination[p] = (byte)(
                // Mask out lower bits
                (destination[p] & (0xFF << (numberOfBits - bitsFree))) |

                // Write the upper bits to the lower bits of the second byte
                (source >> bitsFree)
            );
        }


        /// <summary>
        /// Write given bytes and perform bit shifting as needed
        /// </summary>
        public static void WriteBytes(byte[] source, int sourceByteOffset, int numberOfBytes, byte[] destination, int destBitOffset)
        {
#if UNSAFE
            unsafe
            {
                var destByteOffset = destBitOffset >> 3;
                destBitOffset -= destByteOffset * 8;
                AssertAlways(source.Length >= sourceByteOffset + numberOfBytes);
                AssertAlways(destination.Length >= destByteOffset + (destBitOffset == 0 ? numberOfBytes : (numberOfBytes + 1)));
                fixed (byte* pSource = &source[sourceByteOffset], pDest = &destination[destByteOffset])
                {
                    WriteBytes(pSource, numberOfBytes, pDest, destBitOffset);
                }
            }
#else
            int dstBytePtr = destBitOffset >> 3;
            int segWriteOffset = (destBitOffset % 8);

            if (segWriteOffset == 0)
            {
                Buffer.BlockCopy(source, sourceByteOffset, destination, dstBytePtr, numberOfBytes);
                return;
            }

            int firstPartLen = 8 - segWriteOffset;

            for (int i = 0; i < numberOfBytes; i++)
            {
                byte src = source[sourceByteOffset + i];

                // write last part of this byte
                destination[dstBytePtr] &= (byte)(byte.MaxValue >> firstPartLen); // clear before writing
                destination[dstBytePtr] |= (byte)(src << segWriteOffset); // write first half

                dstBytePtr++;

                // write first part of next byte
                destination[dstBytePtr] &= (byte)(byte.MaxValue << segWriteOffset); // clear before writing
                destination[dstBytePtr] |= (byte)(src >> firstPartLen); // write second half
            }

#endif
        }

#if UNSAFE

        /// <summary>
        /// Write given bytes and perform bit shifting as needed
        /// </summary>
        [CLSCompliant(false)]
        public static unsafe void WriteBytes(byte* source, int cbSource, byte* destination, int destBitOffset)
        {
            Assert(0 <= destBitOffset && destBitOffset < 8);
            if (destBitOffset == 0)
            {
                Buffer.MemoryCopy(source, destination, cbSource, cbSource);
                return;
            }

            int firstPartLen = 8 - destBitOffset;

            for (int i = 0; i < cbSource;)
            {
                byte src = source[i];

                // write last part of this byte
                destination[i] &= (byte)(byte.MaxValue >> firstPartLen); // clear before writing
                destination[i] |= (byte)(src << destBitOffset); // write first half
                i++;
                // write first part of next byte
                destination[i] &= (byte)(byte.MaxValue << destBitOffset); // clear before writing
                destination[i] |= (byte)(src >> firstPartLen); // write second half
            }

            return;
        }

        /// <summary>
        /// Write an unmanaged POCO struct into the buffer
        /// </summary>
        [CLSCompliant(false)]
        public static unsafe void WriteStruct<T>(ref T value, byte* dest, int destBitOffset) where T : unmanaged
        {
            fixed (T* pVal = &value)
            {
                // Byte-aligned, just copy the whole struct
                if (destBitOffset == 0)
                    *(T*)dest = value;
                else
                    WriteBytes((byte*)pVal, sizeof(T), dest, destBitOffset);
            }
        }


        /// <summary>
        /// Write an unmanaged POCO struct into specified buffer
        /// </summary>
        [CLSCompliant(false)]
        public static unsafe void WriteStruct<T>(ref T value, byte[] dest, int destBitOffset) where T : unmanaged
        {
            var destByteOffset = destBitOffset >> 3;
            destBitOffset -= destByteOffset * 8;
            AssertAlways(dest.Length >= (destBitOffset == 0 ? destByteOffset : (destByteOffset + 1)) + sizeof(T));
            fixed (byte* pDest = &dest[destByteOffset])
            {
                WriteStruct(ref value, pDest, destBitOffset);
            }
        }
#endif


        /// <summary>
        /// Reads an unsigned 16 bit integer
        /// </summary>
        [CLSCompliant(false)]
#if UNSAFE
        public static unsafe ushort ReadUInt16(byte[] fromBuffer, int numberOfBits, int readBitOffset)
        {
            Debug.Assert(((numberOfBits > 0) && (numberOfBits <= 16)), "ReadUInt16() can only read between 1 and 16 bits");

            if (numberOfBits == 16 && ((readBitOffset % 8) == 0))
            {
                fixed (byte* ptr = &(fromBuffer[readBitOffset / 8]))
                {
                    return *(((ushort*)ptr));
                }
            }
#else
        public static ushort ReadUInt16(byte[] fromBuffer, int numberOfBits, int readBitOffset)
        {
            Debug.Assert(((numberOfBits > 0) && (numberOfBits <= 16)), "ReadUInt16() can only read between 1 and 16 bits");
#endif
            ushort returnValue;
            if (numberOfBits <= 8)
            {
                returnValue = ReadByte(fromBuffer, numberOfBits, readBitOffset);
                return returnValue;
            }
            returnValue = ReadByte(fromBuffer, 8, readBitOffset);
            numberOfBits -= 8;
            readBitOffset += 8;

            if (numberOfBits <= 8)
            {
                returnValue |= (ushort)(ReadByte(fromBuffer, numberOfBits, readBitOffset) << 8);
            }

#if BIGENDIAN
			// reorder bytes
			uint retVal = returnValue;
			retVal = ((retVal & 0x0000ff00) >> 8) | ((retVal & 0x000000ff) << 8);
			return (ushort)retVal;
#else
            return returnValue;
#endif
        }

        /// <summary>
        /// Reads the specified number of bits into an UInt32
        /// </summary>
        [CLSCompliant(false)]
#if UNSAFE
        public static unsafe uint ReadUInt32(byte[] fromBuffer, int numberOfBits, int readBitOffset)
        {
            Assert(((numberOfBits > 0) && (numberOfBits <= 32)), "ReadUInt32() can only read between 1 and 32 bits");

            if (numberOfBits == 32 && ((readBitOffset % 8) == 0))
            {
                fixed (byte* ptr = &(fromBuffer[readBitOffset / 8]))
                {
                    return *(((uint*)ptr));
                }
            }
#else

        public static uint ReadUInt32(byte[] fromBuffer, int numberOfBits, int readBitOffset)
        {
            Assert(((numberOfBits > 0) && (numberOfBits <= 32)), "ReadUInt32() can only read between 1 and 32 bits");
#endif
            uint returnValue;
            if (numberOfBits <= 8)
            {
                returnValue = ReadByte(fromBuffer, numberOfBits, readBitOffset);
                return returnValue;
            }
            returnValue = ReadByte(fromBuffer, 8, readBitOffset);
            numberOfBits -= 8;
            readBitOffset += 8;

            if (numberOfBits <= 8)
            {
                returnValue |= (uint)(ReadByte(fromBuffer, numberOfBits, readBitOffset) << 8);
                return returnValue;
            }
            returnValue |= (uint)(ReadByte(fromBuffer, 8, readBitOffset) << 8);
            numberOfBits -= 8;
            readBitOffset += 8;

            if (numberOfBits <= 8)
            {
                uint r = ReadByte(fromBuffer, numberOfBits, readBitOffset);
                r <<= 16;
                returnValue |= r;
                return returnValue;
            }
            returnValue |= (uint)(ReadByte(fromBuffer, 8, readBitOffset) << 16);
            numberOfBits -= 8;
            readBitOffset += 8;

            returnValue |= (uint)(ReadByte(fromBuffer, numberOfBits, readBitOffset) << 24);

#if BIGENDIAN
			// reorder bytes
			return
				((returnValue & 0xff000000) >> 24) |
				((returnValue & 0x00ff0000) >> 8) |
				((returnValue & 0x0000ff00) << 8) |
				((returnValue & 0x000000ff) << 24);
#else
            return returnValue;
#endif
        }

        //[CLSCompliant(false)]
        //public static ulong ReadUInt64(byte[] fromBuffer, int numberOfBits, int readBitOffset)

        /// <summary>
        /// Writes an unsigned 16 bit integer
        /// </summary>
        [CLSCompliant(false)]
        public static void WriteUInt16(ushort source, int numberOfBits, byte[] destination, int destinationBitOffset)
        {
            if (numberOfBits == 0)
                return;

            Assert((numberOfBits >= 0 && numberOfBits <= 16), "numberOfBits must be between 0 and 16");
#if BIGENDIAN
			// reorder bytes
			uint intSource = source;
			intSource = ((intSource & 0x0000ff00) >> 8) | ((intSource & 0x000000ff) << 8);
			source = (ushort)intSource;
#endif
            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)source, numberOfBits, destination, destinationBitOffset);
                return;
            }

            NetBitWriter.WriteByte((byte)source, 8, destination, destinationBitOffset);

            numberOfBits -= 8;
            if (numberOfBits > 0)
                NetBitWriter.WriteByte((byte)(source >> 8), numberOfBits, destination, destinationBitOffset + 8);
        }

        /// <summary>
        /// Writes the specified number of bits into a byte array
        /// </summary>
        [CLSCompliant(false)]
        public static int WriteUInt32(uint source, int numberOfBits, byte[] destination, int destinationBitOffset)
        {
#if BIGENDIAN
			// reorder bytes
			source = ((source & 0xff000000) >> 24) |
				((source & 0x00ff0000) >> 8) |
				((source & 0x0000ff00) << 8) |
				((source & 0x000000ff) << 24);
#endif

            int returnValue = destinationBitOffset + numberOfBits;
            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)source, numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)source, 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 8), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 8), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 16), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 16), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            NetBitWriter.WriteByte((byte)(source >> 24), numberOfBits, destination, destinationBitOffset);
            return returnValue;
        }

        /// <summary>
        /// Writes the specified number of bits into a byte array
        /// </summary>
        [CLSCompliant(false)]
        public static int WriteUInt64(ulong source, int numberOfBits, byte[] destination, int destinationBitOffset)
        {
#if BIGENDIAN
			source = ((source & 0xff00000000000000L) >> 56) |
				((source & 0x00ff000000000000L) >> 40) |
				((source & 0x0000ff0000000000L) >> 24) |
				((source & 0x000000ff00000000L) >> 8) |
				((source & 0x00000000ff000000L) << 8) |
				((source & 0x0000000000ff0000L) << 24) |
				((source & 0x000000000000ff00L) << 40) |
				((source & 0x00000000000000ffL) << 56);
#endif

            int returnValue = destinationBitOffset + numberOfBits;
            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)source, numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)source, 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 8), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 8), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 16), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 16), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 24), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 24), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 32), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 32), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 40), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 40), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 48), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 48), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8)
            {
                NetBitWriter.WriteByte((byte)(source >> 56), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 56), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            return returnValue;
        }

        //
        // Variable size
        //

        /// <summary>
        /// Write Base128 encoded variable sized unsigned integer
        /// </summary>
        /// <returns>number of bytes written</returns>
        [CLSCompliant(false)]
        public static int WriteVariableUInt32(byte[] intoBuffer, int offset, uint value)
        {
            int retval = 0;
            uint num1 = (uint)value;
            while (num1 >= 0x80)
            {
                intoBuffer[offset + retval] = (byte)(num1 | 0x80);
                num1 = num1 >> 7;
                retval++;
            }
            intoBuffer[offset + retval] = (byte)num1;
            return retval + 1;
        }

        /// <summary>
        /// Reads a UInt32 written using WriteUnsignedVarInt(); will increment offset!
        /// </summary>
        [CLSCompliant(false)]
        public static uint ReadVariableUInt32(byte[] buffer, ref int offset)
        {
            int num1 = 0;
            int num2 = 0;
            while (true)
            {
                Assert(num2 != 0x23, "Bad 7-bit encoded integer");

                byte num3 = buffer[offset++];
                num1 |= (num3 & 0x7f) << (num2 & 0x1f);
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return (uint)num1;
            }
        }
    }
}
