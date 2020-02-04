constant size_t EnvelopeSize = 10;
constant float Epsilon = 0.01;
constant float EpsilonPlus = 1.01; //1 + Epsilon;

float GetUpTo(float ATime, float AK)
{
	return (1 - 1 / (1 + AK * ATime * ATime)) * EpsilonPlus;
}

float GetDownTo(float ATime, float AK)
{
	return EpsilonPlus / (1 + AK * ATime * ATime) - Epsilon;
}

float Envelope(float time, global read_only float *env)
{
	float FAttakK = env[0];
	float FAttacTime = env[1];

	float FDecayK = env[2];
	float FDecayTime = env[3];

	float FReleaseK = env[4];
	float FReleaseTime = env[5];

	float FStartTime = env[6];
	float FStopTime = env[7];
	float FStopValue = env[8];
	float SustainLevel = env[9];

	if (FStartTime == -1)
		return 0;

	float t;
	if (FStopTime == -1)
	{
		t = time - FStartTime;
		if (t < FAttacTime)
			return GetUpTo(t, FAttakK);
		t -= FAttacTime;
		if (t < FDecayTime)
			return 1 - (1 - SustainLevel) * GetUpTo(t, FDecayK);
		//return SustainLevel + (1 - SustainLevel) * GetDownTo(t, FDecayK);
		return SustainLevel;
	}
	t = time - FStopTime;
	if (t < FReleaseTime)
		return FStopValue * GetDownTo(t, FReleaseK);
	return 0;
}

kernel void Wave(
	global  read_only float* args,
	global  read_only float* ampl,
	global  read_only float* freq,
	global  read_only float* envs,
	global write_only float* result)
{
	// Разбор аргументов
	float startTime = args[0];
	float timeDelta = args[1];
	size_t inclusiveFrom = (size_t)args[2];
	size_t noteCount = (size_t)args[3];
	// Количество точек на ноту (оно одинаковое, т.к. один инструмент)
	size_t pointCount = (size_t)args[4];
	float masterVolume = args[5];

  size_t index = get_global_id(0);  
	float r = 0;
	//float totalAmp = 0;
	float time = startTime + index * timeDelta;

	size_t afIndex = 0;
	for (size_t n = 0; n < noteCount; n++)
	{
		float localAmp = 0;
		float localR = 0;
		for (size_t p = 0; p < pointCount; p++, afIndex++)
		{
			localR = localR + ampl[afIndex]*sin(2*M_PI*freq[afIndex]*time);
			localAmp = localAmp + ampl[afIndex];
		}
		if (localAmp > 1)
			localR = localR / localAmp;
		localR *= Envelope(time, &envs[n*EnvelopeSize]);
		r += localR;
	}

	r *= masterVolume;

	index *= 2;
	result[index] = r;
	index++;
	result[index] = r;
}