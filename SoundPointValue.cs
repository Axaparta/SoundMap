namespace SoundMap
{
	public struct SoundPointValue
	{
		public double Left { get; set; }
		public double Right { get; set; }

		public SoundPointValue(double ALeft, double ARight)
		{
			Left = ALeft;
			Right = ARight;
		}

		public static SoundPointValue operator *(double AMultipler, SoundPointValue AValue)
		{
			return new SoundPointValue(AMultipler * AValue.Left, AMultipler * AValue.Right);
		}

		public static SoundPointValue operator *(SoundPointValue AValue, double AMultipler)
		{
			return new SoundPointValue(AMultipler * AValue.Left, AMultipler * AValue.Right);
		}

		public static SoundPointValue operator /(SoundPointValue AValue, double ADivider)
		{
			return new SoundPointValue(AValue.Left / ADivider, AValue.Right / ADivider);
		}

		public static SoundPointValue operator +(SoundPointValue AValueA, SoundPointValue AValueB)
		{
			return new SoundPointValue(AValueA.Left + AValueB.Left, AValueA.Right + AValueB.Right);
		}
	}
}
