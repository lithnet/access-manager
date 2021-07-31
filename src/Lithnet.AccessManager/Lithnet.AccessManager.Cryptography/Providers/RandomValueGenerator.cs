using System.Collections.Generic;
using System.Security.Cryptography;

namespace Lithnet.AccessManager.Cryptography
{
    public class RandomValueGenerator : IRandomValueGenerator
    {
        private readonly RandomNumberGenerator rng;

        private static readonly char[] SymbolCharacterSet =
        {
            '!', '@', '#', '$', '%', '^', '&', '*', '-',
            '+', '=', '<', '>', '?', ',', '.'
        };

        private static readonly char[] NumericCharacterSet =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
        };

        private static readonly char[] UppercaseAlphaCharacterSet =
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M',
            'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        };

        private static readonly char[] LowercaseAlphaCharacterSet =
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm',
            'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
        };

        public RandomValueGenerator(RandomNumberGenerator rng)
        {
            this.rng = rng;
        }

        public string GenerateRandomString(int length)
        {
            return this.GenerateRandomString(length, null, true, true, true, false);
        }

        public string GenerateRandomString(int length, string specificCharacters)
        {
            return this.GenerateRandomString(length, specificCharacters, false, false, false, false);
        }

        public string GenerateRandomString(int length, bool useLower, bool useUpper, bool useNumeric, bool useSymbol)
        {
            return this.GenerateRandomString(length, null, useLower, useUpper, useNumeric, useSymbol);
        }
        
        private string GenerateRandomString(int length, string specificCharacters, bool useLower, bool useUpper, bool useNumeric, bool useSymbol)
        {
            char[] selectedChars = this.BuildCharacterSet(specificCharacters, useLower, useUpper, useNumeric, useSymbol);
            string rawString = this.GenerateRandomString(length, selectedChars);

            return rawString;
        }

        private char[] BuildCharacterSet(string specificCharacters, bool useLower, bool useUpper, bool useNumeric, bool useSymbol)
        {
            List<char> selectedChars = new List<char>();

            if (string.IsNullOrWhiteSpace(specificCharacters))
            {
                if (useLower)
                {
                    selectedChars.AddRange(LowercaseAlphaCharacterSet);
                }

                if (useUpper)
                {
                    selectedChars.AddRange(UppercaseAlphaCharacterSet);
                }

                if (useNumeric)
                {
                    selectedChars.AddRange(NumericCharacterSet);
                }

                if (useSymbol)
                {
                    selectedChars.AddRange(SymbolCharacterSet);
                }
            }
            else
            {
                foreach (char c in specificCharacters)
                {
                    selectedChars.Add(c);
                }
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

        private string GenerateRandomString(int length, params char[] allowedChars)
        {
            char[] identifier = new char[length];
            byte[] randomData = this.GetRandomBytes(length);

            for (int idx = 0; idx < identifier.Length; idx++)
            {
                int pos = randomData[idx] % allowedChars.Length;
                identifier[idx] = allowedChars[pos];
            }

            return new string(identifier);
        }

        private byte[] GetRandomBytes(int size)
        {
            byte[] buffer = new byte[size];
            this.rng.GetBytes(buffer);
            return buffer;
        }
    }
}