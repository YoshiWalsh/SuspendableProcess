using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace SuspendableProcess.CodeFromCoreFX
{
    internal sealed class OSEncoding : Encoding
    {
        private readonly int _codePage;
        private string _encodingName;

        internal OSEncoding(int codePage)
            : base(codePage)
        {
            _codePage = codePage;
        }

        public override string EncodingName
        {
            get
            {
                if (_encodingName == null)
                {
                    _encodingName = "Codepage - " + _codePage.ToString();
                }
                return _encodingName;
            }
        }

        public override string WebName
        {
            get
            {
                return EncodingName;
            }
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count));

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(chars));

            if (count == 0)
                return 0;

            fixed (char* pChar = chars)
            {
                return WideCharToMultiByte(_codePage, pChar + index, count, null, 0);
            }
        }

        public override unsafe int GetByteCount(string s)
        {
            // Validate input
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            if (s.Length == 0)
                return 0;

            fixed (char* pChars = s)
            {
                return WideCharToMultiByte(_codePage, pChars, s.Length, null, 0);
            }
        }

        public override unsafe int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (s == null || bytes == null)
                throw new ArgumentNullException(s == null ? nameof(s) : nameof(bytes));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount));

            if (s.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(s));

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (charCount == 0)
                return 0;

            if (bytes.Length == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            fixed (char* pChars = s)
            fixed (byte* pBytes = &bytes[0])
            {
                return WideCharToMultiByte(_codePage, pChars + charIndex, charCount, pBytes + byteIndex, bytes.Length - byteIndex);
            }
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount));

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars));

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (charCount == 0)
                return 0;

            if (bytes.Length == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            fixed (char* pChars = chars)
            fixed (byte* pBytes = &bytes[0])
            {
                return WideCharToMultiByte(_codePage, pChars + charIndex, charCount, pBytes + byteIndex, bytes.Length - byteIndex);
            }
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count));

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes));

            if (count == 0)
                return 0;

            fixed (byte* pBytes = bytes)
            {
                return MultiByteToWideChar(_codePage, pBytes + index, count, null, 0);
            }
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars));

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount));

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes));

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex));

            if (byteCount == 0)
                return 0;

            if (chars.Length == 0)
                throw new ArgumentOutOfRangeException();

            fixed (byte* pBytes = bytes)
            fixed (char* pChars = &chars[0])
            {
                return MultiByteToWideChar(_codePage, pBytes + byteIndex, byteCount, pChars + charIndex, chars.Length - charIndex);
            }
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount));

            long byteCount = (long)charCount * 14; // Max possible value for all encodings
            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount));

            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            long charCount = byteCount * 4; // Max possible value for all encodings

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            return (int)charCount;
        }

        public override Encoder GetEncoder()
        {
            return new OSEncoder(this);
        }

        public override Decoder GetDecoder()
        {
            switch (CodePage)
            {
                case 932: // Japanese (Shift-JIS)
                case 936: // Chinese Simplified (GB2312)
                case 949: // Korean
                case 950: // Chinese Traditional (Big5)
                case 1361: // Korean (Johab)
                case 10001: // Japanese (Mac)
                case 10002: // Chinese Traditional (Mac)
                case 10003: // Korean (Mac)
                case 10008: // Chinese Simplified (Mac)
                case 20000: // Chinese Traditional (CNS)
                case 20001: // TCA Taiwan
                case 20002: // Chinese Traditional (Eten)
                case 20003: // IBM5550 Taiwan
                case 20004: // TeleText Taiwan
                case 20005: // Wang Taiwan
                case 20261: // T.61
                case 20932: // Japanese (JIS 0208-1990 and 0212-1990)
                case 20936: // Chinese Simplified (GB2312-80)
                case 51949: // Korean (EUC)
                    return new DecoderDBCS(this);

                default:
                    return base.GetDecoder();
            }
        }

        internal static unsafe int WideCharToMultiByte(int codePage, char* pChars, int count, byte* pBytes, int byteCount)
        {
            int result = Interop.Kernel32.WideCharToMultiByte((uint)codePage, 0, pChars, count, pBytes, byteCount, IntPtr.Zero, IntPtr.Zero);
            if (result <= 0)
                throw new ArgumentException();
            return result;
        }

        internal static unsafe int MultiByteToWideChar(int codePage, byte* pBytes, int byteCount, char* pChars, int count)
        {
            int result = Interop.Kernel32.MultiByteToWideChar((uint)codePage, 0, pBytes, byteCount, pChars, count);
            if (result <= 0)
                throw new ArgumentException();
            return result;
        }
    }
}
