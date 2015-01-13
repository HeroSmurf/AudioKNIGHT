using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace AudioKnight
{
	public class SilentSampleProvider : ISampleProvider
	{
		public int Read(float[] buffer, int offset, int count)
		{
			for (int i = offset; i < offset + count; i++)
				buffer[i] = 0;
			return count;
		}

		public WaveFormat WaveFormat
		{
			get { return WaveFormat.CreateIeeeFloatWaveFormat(48000, 1); }
		}
	}
}
