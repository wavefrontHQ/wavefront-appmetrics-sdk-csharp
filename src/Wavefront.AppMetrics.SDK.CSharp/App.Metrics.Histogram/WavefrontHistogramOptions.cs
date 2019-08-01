using System;
using App.Metrics.ReservoirSampling;
using App.Metrics.ReservoirSampling.Wavefront;

namespace App.Metrics.Histogram
{
    public class WavefrontHistogramOptions : HistogramOptions
    {
        private static readonly string WavefrontMetricTypeTagValue = "wavefrontHistogram";

        public class Builder
        {
            // Required parameters
            private readonly string name;
            private readonly Func<IReservoir> reservoir;

            // Optional parameters
            private string context;
            private Unit measurementUnit;
            private MetricTags tags;

            public Builder(string name)
            {
                this.name = name;

                // Use the IReservoir that is specific to Wavefront Histograms
                reservoir = () => new WavefrontHistogramReservoir();

                // Set the default measurement unit, since it cannot be null
                measurementUnit = Unit.None;
            }

            /// <summary>
            ///     Sets the context for the Wavefront Histogram.
            /// </summary>
            /// <returns><see cref="this"/></returns>
            /// <param name="context">The context.</param>
            public Builder Context(string context)
            {
                this.context = context;
                return this;
            }

            /// <summary>
            ///     Sets the measurement unit for the Wavefront Histogram.
            /// </summary>
            /// <returns><see cref="this"/></returns>
            /// <param name="measurementUnit">The measurement unit.</param>
            public Builder MeasurementUnit(Unit measurementUnit)
            {
                this.measurementUnit = measurementUnit;
                return this;
            }

            /// <summary>
            ///     Sets the metric tags for the Wavefront Histogram.
            /// </summary>
            /// <returns><see cref="this"/></returns>
            /// <param name="tags">The metric tags.</param>
            public Builder Tags(MetricTags tags)
            {
                this.tags = tags;
                return this;
            }

            /// <summary>
            ///     Creates a new <see cref="WavefrontHistogramOptions"/> instance. In order for
            ///     the Wavefront Histogram to be reported correctly, the fields for the new
            ///     instance should not be changed after the instance is created.
            /// </summary>
            /// <returns>A new <see cref="WavefrontHistogramOptions"/></returns>
            public WavefrontHistogramOptions Build()
            {
                var options = new WavefrontHistogramOptions()
                {
                    Name = name,
                    Reservoir = reservoir
                };

                if (context != null)
                {
                    options.Context = context;
                }

                options.MeasurementUnit = measurementUnit;

                // The Wavefront reporter identifies Wavefront Histograms by a specific tag
                options.Tags = MetricTags.Concat(tags, new MetricTags(
                    WavefrontConstants.WavefrontMetricTypeTagKey, WavefrontMetricTypeTagValue));

                return options;
            }
        }

        private WavefrontHistogramOptions()
        {
        }

        /// <summary>
        ///     Determines whether or not a metric is in the format of a Wavefront Histogram by
        ///     looking for a particular metric tag.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the tags identify a Wavefront Histogram, <c>false</c> otherwise.
        /// </returns>
        /// <param name="tags">The metric tags.</param>
        public static bool IsWavefrontHistogram(MetricTags tags)
        {
            return WavefrontConstants.IsWavefrontMetricType(tags, WavefrontMetricTypeTagValue);
        }
    }
}
