using System;
using System.Collections.Generic;
using System.Linq;
using Wavefront.SDK.CSharp.Entities.Histograms;

namespace App.Metrics.ReservoirSampling.Wavefront
{
    /// <summary>
    ///     Represents a statistical snapshot of a Wavefront Histogram distribution.
    ///     Because Wavefront Histograms have no concept of user values, and because
    ///     the distribution itself needs to be reported, the user value properties
    ///     have been repurposed to serve as a carrier for the distribution data.
    /// </summary>
    public class WavefrontHistogramSnapshot : IReservoirSnapshot
    {
        private readonly WavefrontHistogramImpl.Snapshot snapshot;
        private readonly KeyValuePair<string, string> distributions;

        /// <summary>
        /// Initializes a new instance of the <see cref="WavefrontHistogramSnapshot"/> class.
        /// </summary>
        /// <param name="snapshot">A Wavefront Histogram snapshot.</param>
        /// <param name="distributions">The serialized Wavefront Histogram distributions.</param>
        public WavefrontHistogramSnapshot(
            WavefrontHistogramImpl.Snapshot snapshot,
            KeyValuePair<string, string> distributions)
        {
            this.snapshot = snapshot;
            this.distributions = distributions;
        }

        /// <inheritdoc />
        public long Count => snapshot.Count;

        /// <inheritdoc />
        public long Max => (long)snapshot.Max;

        /// <summary>
        ///     Holds the timestamps of the distributions.
        /// </summary>
        /// <value>The timestamps serialized as a string.</value>
        public string MaxUserValue => distributions.Key;

        /// <inheritdoc />
        public double Mean => snapshot.Mean;

        /// <inheritdoc />
        public double Median => GetValue(0.5d);

        /// <inheritdoc />
        public long Min => (long)snapshot.Min;

        /// <summary>
        ///     Holds the centroids of the distributions.
        /// </summary>
        /// <value>The centroids serialized as a string.</value>
        public string MinUserValue => distributions.Value;

        /// <inheritdoc />
        public double Percentile75 => GetValue(0.75d);

        /// <inheritdoc />
        public double Percentile95 => GetValue(0.95d);

        /// <inheritdoc />
        public double Percentile98 => GetValue(0.98d);

        /// <inheritdoc />
        public double Percentile99 => GetValue(0.99d);

        /// <inheritdoc />
        public double Percentile999 => GetValue(0.999d);

        /// <inheritdoc />
        public int Size => snapshot.Size;

        /// <summary>
        ///     This property is not supported by Wavefront Histograms.
        /// </summary>
        /// <value>NaN.</value>
        public double StdDev => snapshot.StdDev;

        /// <inheritdoc />
        public double Sum => snapshot.Sum;

        /// <inheritdoc />
        public IEnumerable<long> Values => snapshot.Values.Select(value => (long)value);

        /// <inheritdoc />
        public double GetValue(double quantile) => snapshot.GetValue(quantile);
    }
}
