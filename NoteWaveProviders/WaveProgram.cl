constant float Epsilon = 0.01;
constant float EpsilonPlus = 1.01; //1 + Epsilon;

typedef struct __attribute__ ((packed)) _pointInfo
{
	float ampl;
	float freq;
	float leftPct;
	float rightPct;
	// -1 is sine, no waveform
	int wfIndex;
	int isMute;
} pointInfo;

typedef struct __attribute__ ((packed)) _info
{
	float  startTime;
	float  timeDelta;
	int    sampleRate;
	float  masterVolume;
	uint inclusiveFrom;

	uint noteCount;
	// Points per note
	uint pointCount;
} infoStruct;

typedef struct __attribute__ ((packed)) _envelope
{
	float attakK;
	float attacTime;

	float decayK;
	float decayTime;

	float releaseK;
	float releaseTime;

	float startTime;
	float stopTime;
	float stopValue;
	float sustainLevel;
} envelopeStruct;

typedef struct __attribute__ ((packed)) _noteInfo
{
	envelopeStruct envelope;
	float volume;
} noteInfo;

float GetUpTo(float ATime, float AK)
{
	return (1 - 1 / (1 + AK * ATime * ATime)) * EpsilonPlus;
}

float GetDownTo(float ATime, float AK)
{
	return EpsilonPlus / (1 + AK * ATime * ATime) - Epsilon;
}

float Envelope(float time, envelopeStruct env)
{
	if (env.startTime == -1)
		return 0;

	float t;
	if (env.stopTime == -1)
	{
		t = time - env.startTime;
		if (t < env.attacTime)
			return GetUpTo(t, env.attakK);
		t -= env.attacTime;
		if (t < env.decayTime)
			return 1 - (1 - env.sustainLevel) * GetUpTo(t, env.decayK);
		//return SustainLevel + (1 - SustainLevel) * GetDownTo(t, FDecayK);
		return env.sustainLevel;
	}
	t = time - env.stopTime;
	if (t < env.releaseTime)
		return env.stopValue * GetDownTo(t, env.releaseK);
	return 0;
}

kernel void Wave(
	global read_only infoStruct* infos,
	global read_only pointInfo* points,
	global read_only float* wfs,
	global read_only noteInfo* notes,
	global write_only float* result)
{
	// Разбор аргументов
	infoStruct info = infos[0]; 

  size_t index = get_global_id(0);  
	float r = 0;
	float l = 0;
	//float totalAmp = 0;
	float time = info.startTime + index * info.timeDelta;

	size_t afIndex = 0;
	// В рамках одной ноты
	for (size_t n = 0; n < info.noteCount; n++)
	{
		float localAmp = 0;
		float localLR;
		float localR = 0;
		float localL = 0;
		float env;
		// Точки по ноте
		for (size_t p = 0; p < info.pointCount; p++, afIndex++)
		{
			if (points[afIndex].isMute == 1)
				continue;

			if (points[afIndex].wfIndex > -1)
			{
				localLR = wfs[
					// Смещение формы относительно начала
					(ulong)info.sampleRate*(ulong)points[afIndex].wfIndex +
					(ulong)(time * info.sampleRate * points[afIndex].freq) % (ulong)info.sampleRate];
			}
			else
				localLR = sin(2*M_PI*points[afIndex].freq*time);

			localLR *= points[afIndex].ampl;

			localR = localR + points[afIndex].rightPct * localLR;
			localL = localL + points[afIndex].leftPct * localLR;

			localAmp = localAmp + points[afIndex].ampl;
		}
		if (localAmp > 1)
		{
			localR = localR / localAmp;
			localL = localL / localAmp;
		}
		env = notes[n].volume * Envelope(time, notes[n].envelope);
		r += localR*env;
		l += localL*env;
	}

	r *= info.masterVolume;
	l *= info.masterVolume;

	index *= 2;
	result[index] = l;
	index++;
	result[index] = r;
}