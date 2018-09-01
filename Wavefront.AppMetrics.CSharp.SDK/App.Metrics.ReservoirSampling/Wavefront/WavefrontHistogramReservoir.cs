using System;
using Wavefront.CSharp.SDK.Entities.Histograms;

namespace App.Metrics.ReservoirSampling.Wavefront
{
    public class WavefrontHistogramReservoir : IReservoir
    {
        private readonly WavefrontHistogramImpl wavefrontHistogram;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WavefrontHistogramReservoir"/> class.
        ///     Backed by a <see cref="WavefrontHistogramImpl"/>.
        /// </summary>
        public WavefrontHistogramReservoir()
        {
            wavefrontHistogram = new WavefrontHistogramImpl();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WavefrontHistogramReservoir"/> class.
        ///     Backed by a <see cref="WavefrontHistogramImpl"/>.
        /// </summary>
        /// <param name="clockMillis">
        ///     A delegate function that simulates a clock for taking timestamps in milliseconds.
        /// </param>
        public WavefrontHistogramReservoir(Func<long> clockMillis)
        {
            wavefrontHistogram = new WavefrontHistogramImpl(clockMillis);
        }

        /// <inheritdoc />
        public IReservoirSnapshot GetSnapshot(bool resetReservoir)
        {
            return GetSnapshot();
        }

        /// <inheritdoc />
        public IReservoirSnapshot GetSnapshot()
        {
            return new WavefrontHistogramSnapshot(
                wavefrontHistogram.GetSnapshot(),
                WavefrontHistogramImpl.Serialize(wavefrontHistogram.FlushDistributions())
            );
        }

        /// <summary>
        ///     This method is not supported by Wavefront Histogarms.
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException("Reset() is not supported by Wavefront Histograms.");
        }

        /// <summary>
        ///     This method is not supported by Wavefront Histogarms.
        /// </summary>
        public void Update(long value, string userValue)
        {
            throw new NotSupportedException(
                "Update(long value, string userValue) is not supported by Wavefront Histograms. " +
                "Use Update(long value) instead.");
        }

        /// <inheritdoc />
        public void Update(long value)
        {
            wavefrontHistogram.Update(value);
        }
    }
}
