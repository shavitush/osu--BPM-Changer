// based on EnvelopeDetector.cpp v1.10 © 2006, ChunkWare Music Software, OPEN-SOURCE

using System;
using System.Diagnostics;

namespace NAudio.Dsp
{
    internal class EnvelopeDetector
    {
        private double coeff;
        private double ms;
        private double sampleRate;

        public EnvelopeDetector() : this(1.0, 44100.0)
        {
        }

        public EnvelopeDetector(double ms, double sampleRate)
        {
            Debug.Assert(sampleRate > 0.0);
            Debug.Assert(ms > 0.0);
            this.sampleRate = sampleRate;
            this.ms = ms;
            setCoef();
        }

        public double TimeConstant
        {
            get { return ms; }
            set
            {
                Debug.Assert(value > 0.0);
                ms = value;
                setCoef();
            }
        }

        public double SampleRate
        {
            get { return sampleRate; }
            set
            {
                Debug.Assert(value > 0.0);
                sampleRate = value;
                setCoef();
            }
        }

        public void run(double inValue, ref double state)
        {
            state = inValue + coeff*(state - inValue);
        }

        private void setCoef()
        {
            coeff = Math.Exp(-1.0/(0.001*ms*sampleRate));
        }
    }

    internal class AttRelEnvelope
    {
        // DC offset to prevent denormal
        protected const double DC_OFFSET = 1.0E-25;

        private readonly EnvelopeDetector attack;
        private readonly EnvelopeDetector release;

        public AttRelEnvelope(double att_ms, double rel_ms, double sampleRate)
        {
            attack = new EnvelopeDetector(att_ms, sampleRate);
            release = new EnvelopeDetector(rel_ms, sampleRate);
        }

        public double Attack
        {
            get { return attack.TimeConstant; }
            set { attack.TimeConstant = value; }
        }

        public double Release
        {
            get { return release.TimeConstant; }
            set { release.TimeConstant = value; }
        }

        public double SampleRate
        {
            get { return attack.SampleRate; }
            set { attack.SampleRate = release.SampleRate = value; }
        }

        public void Run(double inValue, ref double state)
        {
            // assumes that:
            // positive delta = attack
            // negative delta = release
            // good for linear & log values
            if (inValue > state)
                attack.run(inValue, ref state); // attack
            else
                release.run(inValue, ref state); // release
        }
    }
}