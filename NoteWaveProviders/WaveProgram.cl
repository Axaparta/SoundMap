kernel void Wave(
	global  read_only float* args,
	global  read_only float* ampl,
	global  read_only float* freq,
	global write_only float* result)
{
  size_t index = get_global_id(0);
	float startTime = args[0];
	float timeDelta = args[1];
	size_t inclusiveFrom = (size_t)args[2];
	size_t pointCount = (size_t)args[3];
  
	float r = 0;
	float totalAmp = 0;
	float time = startTime + index * timeDelta;
	for (int i = 0; i < pointCount; i++)
	{
		r = r + ampl[i]*sin(2*M_PI*freq[i]*time);
		totalAmp = totalAmp + ampl[i];
	}

	if (totalAmp > 1)
		r = r / totalAmp;

	index *= 2;
	result[index] = r;
	index++;
	result[index] = r;
}