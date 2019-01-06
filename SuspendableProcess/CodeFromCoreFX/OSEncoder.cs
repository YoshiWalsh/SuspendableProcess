﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuspendableProcess.CodeFromCoreFX
{
    internal sealed class OSEncoder : Encoder
    {
        private const char NULL_CHAR = (char)0;
        private readonly Encoding _encoding;
        private char _charLeftOver;

        internal OSEncoder(Encoding encoding)
        {
            _encoding = encoding;
            Reset();
        }

        public override void Reset()
        {
            _charLeftOver = NULL_CHAR;
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count));

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(chars));

            if (chars.Length == 0 && (_charLeftOver == NULL_CHAR || !flush))
                return 0;

            fixed (char* pChar = chars)
            {
                char dummyChar;
                char* pBuffer = pChar == null ? &dummyChar : pChar + index;
                return GetByteCount(pBuffer, count, flush);
            }
        }

        public unsafe override int GetByteCount(char* chars, int count, bool flush)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            bool excludeLastChar = count > 0 && !flush && char.IsHighSurrogate(chars[count - 1]);

            if (excludeLastChar)
                count--;

            if (_charLeftOver == NULL_CHAR)
            {
                if (count <= 0)
                    return 0;

                return OSEncoding.WideCharToMultiByte(_encoding.CodePage, chars, count, null, 0);
            }

            // we have left over character
            if (count == 0 && !excludeLastChar && !flush)
                return 0;

            return ConvertWithLeftOverChar(chars, count, null, 0);
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount));

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars));

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (bytes.Length == 0)
                return 0;

            if (charCount == 0 && (_charLeftOver == NULL_CHAR || !flush))
                return 0;

            fixed (char* pChars = chars)
            fixed (byte* pBytes = &bytes[0])
            {
                char dummyChar;
                char* pBuffer = pChars == null ? &dummyChar : pChars + charIndex;

                return GetBytes(pBuffer, charCount, pBytes + byteIndex, bytes.Length - byteIndex, flush);
            }
        }

        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes));

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(byteCount < 0 ? nameof(byteCount) : nameof(charCount));

            if (byteCount == 0)
                return 0;

            char lastChar = charCount > 0 && !flush && char.IsHighSurrogate(chars[charCount - 1]) ? chars[charCount - 1] : NULL_CHAR;

            if (lastChar != NULL_CHAR)
                charCount--;

            if (_charLeftOver == NULL_CHAR)
            {
                if (charCount <= 0)
                {
                    _charLeftOver = lastChar;
                    return 0;
                }

                int result = OSEncoding.WideCharToMultiByte(_encoding.CodePage, chars, charCount, bytes, byteCount);
                _charLeftOver = lastChar;
                return result;
            }

            // we have left over character
            if (charCount == 0 && lastChar == NULL_CHAR && !flush)
                return 0;

            int res = ConvertWithLeftOverChar(chars, charCount, bytes, byteCount);
            _charLeftOver = lastChar;
            return res;
        }

        public override unsafe void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount));

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount));

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars));

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes));

            if (bytes.Length == 0 || (chars.Length == 0 && (_charLeftOver == NULL_CHAR || !flush)))
            {
                bytesUsed = 0;
                charsUsed = 0;
                completed = false;
                return;
            }

            fixed (char* pChars = chars)
            fixed (byte* pBytes = &bytes[0])
            {
                char dummyChar;
                char* pBuffer = pChars == null ? &dummyChar : pChars + charIndex;

                Convert(pBuffer, charCount, pBytes + byteIndex, byteCount, flush, out charsUsed, out bytesUsed, out completed);
            }
        }

        public override unsafe void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars));
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(charCount < 0 ? nameof(charCount) : nameof(byteCount));

            int count = charCount;
            while (count > 0)
            {
                int returnedByteCount = GetByteCount(chars, count, flush);
                if (returnedByteCount <= byteCount)
                    break;

                count /= 2;
            }

            if (count > 0)
            {
                // note GetBytes can change the _charLeftOver state
                bytesUsed = GetBytes(chars, count, bytes, byteCount, flush);
                charsUsed = count;
                completed = _charLeftOver == NULL_CHAR && charCount == count;
                return;
            }

            bytesUsed = 0;
            charsUsed = 0;
            completed = false;
        }

        private unsafe int ConvertWithLeftOverChar(char* chars, int count, byte* bytes, int byteCount)
        {
            Debug.Assert(_charLeftOver != NULL_CHAR, "Char left over is null", "Char left over must not be null char");
            char* pTempBuffer = stackalloc char[2];
            pTempBuffer[0] = _charLeftOver;

            int index = 0;

            if (count > 0 && char.IsLowSurrogate(chars[0]))
            {
                pTempBuffer[1] = chars[0];
                index++;
            }

            int result = OSEncoding.WideCharToMultiByte(_encoding.CodePage, pTempBuffer, index + 1, bytes, byteCount);

            if (count - index > 0)
            {
                result += OSEncoding.WideCharToMultiByte(
                    _encoding.CodePage,
                    chars + index,
                    count - index,
                    bytes == null ? null : bytes + result,
                    bytes == null ? 0 : byteCount - result);
            }

            return result;
        }
    }
}
