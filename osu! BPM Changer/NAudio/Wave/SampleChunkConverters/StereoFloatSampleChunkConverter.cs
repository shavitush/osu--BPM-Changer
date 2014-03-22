using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    internal class StereoFloatSampleChunkConverter : ISampleChunkConverter
    {
        private byte[] sourceBuffer;
        private int sourceSample;
        private int sourceSamples;
        private WaveBuffer sourceWaveBuffer;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
                   waveFormat.Channels == 2;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired*8;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired)/4;
            sourceSample = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.FloatBuffer[sourceSample++];
                sampleRight = sourceWaveBuffer.FloatBuffer[sourceSample++];
                return true;
            }
            sampleLeft = 0.0f;
            sampleRight = 0.0f;
            return false;
        }
    }
}