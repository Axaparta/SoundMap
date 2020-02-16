using System;
using System.Diagnostics.Contracts;

namespace Common
{
	/// <summary>
	/// https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/20695932#20695932
	/// </summary>
	public static class HexHelper
	{
		[Pure]
		public static unsafe string ToHex(this byte[] value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			const string alphabet = @"0123456789ABCDEF";

			string result = new string(' ', checked(value.Length * 2));
			fixed (char* alphabetPtr = alphabet)
			fixed (char* resultPtr = result)
			{
				char* ptr = resultPtr;
				unchecked
				{
					for (int i = 0; i < value.Length; i++)
					{
						*ptr++ = *(alphabetPtr + (value[i] >> 4));
						*ptr++ = *(alphabetPtr + (value[i] & 0xF));
					}
				}
			}
			return result;
		}

		[Pure]
		public static unsafe byte[] FromHex(this string value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.Length % 2 != 0)
				throw new ArgumentException("Hexadecimal value length must be even.", "value");

			unchecked
			{
				byte[] result = new byte[value.Length / 2];
				fixed (char* valuePtr = value)
				{
					char* valPtr = valuePtr;
					for (int i = 0; i < result.Length; i++)
					{
						// 0(48) - 9(57) -> 0 - 9
						// A(65) - F(70) -> 10 - 15
						int b = *valPtr++; // High 4 bits.
						int val = ((b - '0') + ((('9' - b) >> 31) & -7)) << 4;
						b = *valPtr++; // Low 4 bits.
						val += (b - '0') + ((('9' - b) >> 31) & -7);
						result[i] = checked((byte)val);
					}
				}
				return result;
			}
		}
	}
}
