using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Lithnet.AccessManager.Agent;

namespace Lithnet.AccessManager
{
    public class RandomPasswordGenerator : IPasswordGenerator
    {
        private readonly ILapsSettings settings;

        private readonly RNGCryptoServiceProvider csp;

        private static readonly char[] SymbolCharacterSet = {
            '!', '@', '#', '$', '%', '^', '&', '*', '-',
            '+', '=', '<', '>', '?', ',', '.' };

        private static readonly char[] NumericCharacterSet = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

        private static readonly char[] UppercaseAlphaCharacterSet = {
             'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M',
            'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
              };

        private static readonly char[] LowercaseAlphaCharacterSet = {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm',
            'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
              };

        public RandomPasswordGenerator(ILapsSettings settings, RNGCryptoServiceProvider csp)
        {
            this.settings = settings;
            this.csp = csp;
        }

        public string Generate()
        {
            return this.GenerateRandomString(Math.Max(this.settings.PasswordLength, 8));
        }

        private string GenerateRandomString(int length)
        {
            char[] selectedChars = GetSelectedCharacters();

            string rawString = GenerateRandomString(length, selectedChars);

            if (this.settings.UseReadibilitySeparator)
            {
                var split = this.Split(rawString, Math.Max(this.settings.ReadabilitySeparatorInterval, 3));
                rawString = string.Join(this.settings.ReadabilitySeparator ?? "-", split);
            }

            return rawString;
        }

        private char[] GetSelectedCharacters()
        {
            List<char> selectedChars = new List<char>();

            if (this.settings.UseLower)
            {
                selectedChars.AddRange(LowercaseAlphaCharacterSet);
            }

            if (this.settings.UseUpper)
            {
                selectedChars.AddRange(UppercaseAlphaCharacterSet);
            }

            if (this.settings.UseNumeric)
            {
                selectedChars.AddRange(NumericCharacterSet);
            }

            if (this.settings.UseSymbol)
            {
                selectedChars.AddRange(SymbolCharacterSet);
            }

            if (selectedChars.Count == 0)
            {
                selectedChars.AddRange(LowercaseAlphaCharacterSet);
                selectedChars.AddRange(NumericCharacterSet);
                selectedChars.AddRange(UppercaseAlphaCharacterSet);
                selectedChars.AddRange(SymbolCharacterSet);
            }

            return selectedChars.ToArray();
        }

        private IEnumerable<string> Split(string input, int size)
        {
            for (int i = 0; i < input.Length; i += size)
                yield return input.Substring(i, Math.Min(size, input.Length - i));
        }

        private string GenerateRandomString(int length, params char[] allowedChars)
        {
            char[] identifier = new char[length];
            byte[] randomData = GetRandomBytes(length);

            for (int idx = 0; idx < identifier.Length; idx++)
            {
                int pos = randomData[idx] % allowedChars.Length;
                identifier[idx] = allowedChars[pos];
            }

            return new string(identifier);
        }

        private long GenerateRandomNumber(int length)
        {
            ulong minValue = ulong.Parse("1".PadRight(length, '0'));
            ulong maxValue = ulong.Parse("1".PadRight(length + 1, '0'));

            return (long)GetValue(minValue, maxValue);
        }

        private long GenerateRandomNumber()
        {
            return GenerateRandomNumber(8);
        }

        private ulong GetValue(ulong minValue, ulong maxValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            if (maxValue > long.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            ulong diff = (ulong)maxValue - minValue;

            ulong upperBound = ulong.MaxValue / diff * diff;

            ulong value;
            do
            {
                value = GetRandomUInt64();
            } while (value >= upperBound);
            return (ulong)(minValue + (value % diff));
        }

        private ulong GetRandomUInt64()
        {
            byte[] randomBytes = GetRandomBytes(sizeof(ulong));
            return BitConverter.ToUInt64(randomBytes, 0);
        }

        private byte[] GetRandomBytes(int size)
        {
            byte[] buffer = new byte[size];
            csp.GetBytes(buffer);
            return buffer;
        }
    }
}
