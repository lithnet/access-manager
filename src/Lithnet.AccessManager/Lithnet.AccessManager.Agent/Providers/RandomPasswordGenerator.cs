using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Lithnet.AccessManager.Agent.Providers;

namespace Lithnet.AccessManager.Agent
{
    public class RandomPasswordGenerator : IPasswordGenerator
    {
        private readonly ISettingsProvider settings;
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

        public RandomPasswordGenerator(ISettingsProvider settings, RNGCryptoServiceProvider csp)
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
            char[] selectedChars = this.GetSelectedCharacters();

            string rawString = this.GenerateRandomString(length, selectedChars);

            return rawString;
        }

        private char[] GetSelectedCharacters()
        {
            List<char> selectedChars = new List<char>();

            if (string.IsNullOrWhiteSpace(this.settings.PasswordCharacters))
            {
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
            }
            else
            {
                foreach (char c in this.settings.PasswordCharacters)
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

        private IEnumerable<string> Split(string input, int size)
        {
            for (int i = 0; i < input.Length; i += size)
                yield return input.Substring(i, Math.Min(size, input.Length - i));
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
            this.csp.GetBytes(buffer);
            return buffer;
        }
    }
}
