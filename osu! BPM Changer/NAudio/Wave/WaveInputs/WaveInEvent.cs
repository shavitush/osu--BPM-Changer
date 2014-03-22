﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Mixer;

namespace NAudio.Wave
{
    /// <summary>
    ///     Recording using waveIn api with event callbacks.
    ///     Use this for recording in non-gui applications
    ///     Events are raised as recorded buffers are made available
    /// </summary>
    public class WaveInEvent : IWaveIn
    {
        private readonly AutoResetEvent callbackEvent;
        private readonly SynchronizationContext syncContext;
        private WaveInBuffer[] buffers;
        private volatile bool recording;
        private IntPtr waveInHandle;

        /// <summary>
        ///     Prepares a Wave input device for recording
        /// </summary>
        public WaveInEvent()
        {
            callbackEvent = new AutoResetEvent(false);
            syncContext = SynchronizationContext.Current;
            DeviceNumber = 0;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            NumberOfBuffers = 3;
        }

        /// <summary>
        ///     Returns the number of Wave In devices available in the system
        /// </summary>
        public static int DeviceCount
        {
            get { return WaveInterop.waveInGetNumDevs(); }
        }

        /// <summary>
        ///     Milliseconds for the buffer. Recommended value is 100ms
        /// </summary>
        public int BufferMilliseconds { get; set; }

        /// <summary>
        ///     Number of Buffers to use (usually 2 or 3)
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        ///     The device number to use
        /// </summary>
        public int DeviceNumber { get; set; }

        /// <summary>
        ///     Indicates recorded data is available
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        ///     Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        ///     Start recording
        /// </summary>
        public void StartRecording()
        {
            if (recording)
                throw new InvalidOperationException("Already recording");
            OpenWaveInDevice();
            MmException.Try(WaveInterop.waveInStart(waveInHandle), "waveInStart");
            recording = true;
            ThreadPool.QueueUserWorkItem(state => RecordThread(), null);
        }

        /// <summary>
        ///     Stop recording
        /// </summary>
        public void StopRecording()
        {
            recording = false;
            callbackEvent.Set(); // signal the thread to exit
            MmException.Try(WaveInterop.waveInStop(waveInHandle), "waveInStop");
        }

        /// <summary>
        ///     WaveFormat we are recording in
        /// </summary>
        public WaveFormat WaveFormat { get; set; }

        /// <summary>
        ///     Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Retrieves the capabilities of a waveIn device
        /// </summary>
        /// <param name="devNumber">Device to test</param>
        /// <returns>The WaveIn device capabilities</returns>
        public static WaveInCapabilities GetCapabilities(int devNumber)
        {
            var caps = new WaveInCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr) devNumber, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        private void CreateBuffers()
        {
            // Default to three buffers of 100ms each
            int bufferSize = BufferMilliseconds*WaveFormat.AverageBytesPerSecond/1000;
            if (bufferSize%WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize%WaveFormat.BlockAlign;
            }

            buffers = new WaveInBuffer[NumberOfBuffers];
            for (int n = 0; n < buffers.Length; n++)
            {
                buffers[n] = new WaveInBuffer(waveInHandle, bufferSize);
            }
        }

        private void OpenWaveInDevice()
        {
            CloseWaveInDevice();
            MmResult result = WaveInterop.waveInOpenWindow(out waveInHandle, (IntPtr) DeviceNumber, WaveFormat,
                callbackEvent.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero,
                WaveInterop.WaveInOutOpenFlags.CallbackEvent);
            MmException.Try(result, "waveInOpen");
            CreateBuffers();
        }

        private void RecordThread()
        {
            Exception exception = null;
            try
            {
                DoRecording();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                recording = false;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        private void DoRecording()
        {
            foreach (WaveInBuffer buffer in buffers)
            {
                if (!buffer.InQueue)
                {
                    buffer.Reuse();
                }
            }
            while (recording)
            {
                if (callbackEvent.WaitOne())
                {
                    // requeue any buffers returned to us
                    if (recording)
                    {
                        foreach (WaveInBuffer buffer in buffers)
                        {
                            if (buffer.Done)
                            {
                                if (DataAvailable != null)
                                {
                                    DataAvailable(this, new WaveInEventArgs(buffer.Data, buffer.BytesRecorded));
                                }
                                buffer.Reuse();
                            }
                        }
                    }
                }
            }
        }

        private void RaiseRecordingStoppedEvent(Exception e)
        {
            EventHandler<StoppedEventArgs> handler = RecordingStopped;
            if (handler != null)
            {
                if (syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        /// <summary>
        ///     Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (recording)
                    StopRecording();

                CloseWaveInDevice();
            }
        }

        private void CloseWaveInDevice()
        {
            // Some drivers need the reset to properly release buffers
            WaveInterop.waveInReset(waveInHandle);
            if (buffers != null)
            {
                for (int n = 0; n < buffers.Length; n++)
                {
                    buffers[n].Dispose();
                }
                buffers = null;
            }
            WaveInterop.waveInClose(waveInHandle);
            waveInHandle = IntPtr.Zero;
        }

        /// <summary>
        ///     Microphone Level
        /// </summary>
        public MixerLine GetMixerLine()
        {
            // TODO use mixerGetID instead to see if this helps with XP
            MixerLine mixerLine;
            if (waveInHandle != IntPtr.Zero)
            {
                mixerLine = new MixerLine(waveInHandle, 0, MixerFlags.WaveInHandle);
            }
            else
            {
                mixerLine = new MixerLine((IntPtr) DeviceNumber, 0, MixerFlags.WaveIn);
            }
            return mixerLine;
        }
    }
}