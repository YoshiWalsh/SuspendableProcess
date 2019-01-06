using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace SuspendableProcess.CodeFromCoreFX
{
    // This code is taken from the .NET CoreFX project (https://github.com/dotnet/corefx) and is used in accordance with the MIT License. See the LICENSE.MD document in this directory for more details.

    // StreamWriter calls Encoding.GetPreamble() to write the initial bits to the stream.
    // In case of Console we do not want to write the preamble as the user does not expect these bits.
    // In desktop this is handled by setting an internal property on the StreamWriter HasPreambleBeenWritten = true
    // Since portable library does not have access to the internal property we wrap the encoding into a ConsoleEncoding
    // which delegates every call to the original encoding except GetPreamble() which does not do anything.
    internal sealed class ConsoleEncoding : Encoding
    {
        private readonly Encoding _encoding;

        internal ConsoleEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public override int CodePage
        {
            get { return _encoding.CodePage; }
        }

        public override bool IsSingleByte
        {
            get { return _encoding.IsSingleByte; }
        }

        public override string EncodingName
        {
            get { return _encoding.EncodingName; }
        }

        public override string WebName
        {
            get { return _encoding.WebName; }
        }

        public override byte[] GetPreamble()
        {
            return new byte[0];
        }

        public override int GetByteCount(char[] chars)
        {
            return _encoding.GetByteCount(chars);
        }

        public override unsafe int GetByteCount(char* chars, int count)
        {
            return _encoding.GetByteCount(chars, count);
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return _encoding.GetByteCount(chars, index, count);
        }

        public override int GetByteCount(string s)
        {
            return _encoding.GetByteCount(s);
        }

        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            return _encoding.GetBytes(chars, charCount, bytes, byteCount);
        }

        public override byte[] GetBytes(char[] chars)
        {
            return _encoding.GetBytes(chars);
        }

        public override byte[] GetBytes(char[] chars, int index, int count)
        {
            return _encoding.GetBytes(chars, index, count);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return _encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override byte[] GetBytes(string s)
        {
            return _encoding.GetBytes(s);
        }

        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return _encoding.GetBytes(s, charIndex, charCount, bytes, byteIndex);
        }

        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            return _encoding.GetCharCount(bytes, count);
        }

        public override int GetCharCount(byte[] bytes)
        {
            return _encoding.GetCharCount(bytes);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return _encoding.GetCharCount(bytes, index, count);
        }

        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            return _encoding.GetChars(bytes, byteCount, chars, charCount);
        }

        public override char[] GetChars(byte[] bytes)
        {
            return _encoding.GetChars(bytes);
        }

        public override char[] GetChars(byte[] bytes, int index, int count)
        {
            return _encoding.GetChars(bytes, index, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public override Decoder GetDecoder()
        {
            return _encoding.GetDecoder();
        }

        public override Encoder GetEncoder()
        {
            return _encoding.GetEncoder();
        }

        public override int GetMaxByteCount(int charCount)
        {
            return _encoding.GetMaxByteCount(charCount);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return _encoding.GetMaxCharCount(byteCount);
        }

        public override string GetString(byte[] bytes)
        {
            return _encoding.GetString(bytes);
        }

        public override string GetString(byte[] bytes, int index, int count)
        {
            return _encoding.GetString(bytes, index, count);
        }
    }
}
