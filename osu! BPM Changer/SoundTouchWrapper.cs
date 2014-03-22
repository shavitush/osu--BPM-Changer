using System;
using System.Runtime.InteropServices;

namespace osu__BPM_Changer
{
    class SoundTouchWrapper : IDisposable
    {
        private IntPtr m_handle = IntPtr.Zero;

        public void CreateInstance()
        {
            m_handle = soundtouch_createInstance();
        }

        public void Dispose()
        {
            soundtouch_destroyInstance(m_handle);
            m_handle = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sets new rate control value. Normal rate = 1.0, smaller values
        /// represent slower rate, larger faster rates.
        /// </summary>
        /// <param name="newRate"></param>
        public void SetRate(float newRate)
        {
            soundtouch_setRate(m_handle, newRate);
        }

        /// <summary>
        /// Sets new tempo control value. Normal tempo = 1.0, smaller values
        /// represent slower tempo, larger faster tempo.
        /// </summary>
        /// <param name="newTempo"></param>
        public void SetTempo(float newTempo)
        {
            soundtouch_setTempo(m_handle, newTempo);
        }

        /// <summary>
        /// Sets new rate control value as a difference in percents compared
        /// to the original rate (-50 .. +100 %);
        /// </summary>
        /// <param name="newRate"></param>
        public void SetRateChange(float newRate)
        {
            soundtouch_setRateChange(m_handle, newRate);
        }

        /// <summary>
        /// Sets new tempo control value as a difference in percents compared
        /// to the original tempo (-50 .. +100 %)
        /// </summary>
        /// <param name="newRate"></param>
        public void SetTempoChange(float newTempo)
        {
            soundtouch_setTempoChange(m_handle, newTempo);
        }

        public void SetChannels(int numChannels)
        {
            soundtouch_setChannels(m_handle, (uint)numChannels);
        }

        public void SetSampleRate(int srate)
        {
            soundtouch_setSampleRate(m_handle, (uint)srate);
        }

        public void PutSamples(float[] pSamples, uint numSamples)
        {
            soundtouch_putSamples(m_handle, pSamples, numSamples);
        }

        public void SetSetting(SoundTouchSettings settingId, int value)
        {
            soundtouch_setSetting(m_handle, (int)settingId, value);
        }

        public uint ReceiveSamples(float[] pOutBuffer, uint maxSamples)
        {
            return soundtouch_receiveSamples(m_handle, pOutBuffer, maxSamples);
        }

        public enum SoundTouchSettings
        {
            SETTING_USE_AA_FILTER = 0,
            SETTING_AA_FILTER_LENGTH = 1,
            SETTING_USE_QUICKSEEK = 2,
            SETTING_SEQUENCE_MS = 3,
            SETTING_SEEKWINDOW_MS = 4,
            SETTING_OVERLAP_MS = 5
        };

        public const string SoundTouchDLLName = "SoundTouch.dll";

        [DllImport(SoundTouchDLLName)]
        internal static extern IntPtr soundtouch_createInstance();

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_destroyInstance(IntPtr h);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setRate(IntPtr h, float newRate);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setTempo(IntPtr h, float newTempo);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setRateChange(IntPtr h, float newRate);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setTempoChange(IntPtr h, float newTempo);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setChannels(IntPtr h, uint numChannels);

        /// Sets sample rate.
        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setSampleRate(IntPtr h, uint srate);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_putSamples(IntPtr h, [MarshalAs(UnmanagedType.LPArray)] float[] samples, uint numSamples);

        [DllImport(SoundTouchDLLName)]
        internal static extern bool soundtouch_setSetting(IntPtr h, int settingId, int value);

        [DllImport(SoundTouchDLLName)]
        internal static extern uint soundtouch_receiveSamples(IntPtr h, [MarshalAs(UnmanagedType.LPArray)] float[] outBuffer, uint maxSamples);
    }
}
