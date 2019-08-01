namespace App.Metrics.Counter
{
    /// <summary>
    ///     Custom Wavefront configuration of a <see cref="ICounter" /> that will be reported
    ///     as a Wavefront delta counter (whose delta values are aggregated by the Wavefront
    ///     server).
    ///     NOTE: Use <see cref="Builder"/> to configure and build 
    ///     <see cref="DeltaCounterOptions"/>. In order for a delta counter to be reported
    ///     correctly, the fields of a <see cref="DeltaCounterOptions"/> instance should not be
    ///     changed after it has been built.
    /// </summary>
    /// <seealso cref="CounterOptions" />
    public class DeltaCounterOptions : CounterOptions
    {
        private static readonly string WavefrontMetricTypeTagValue = "deltaCounter";

        /// <summary>
        /// Builder for DeltaCounterOptions.
        /// </summary>
        public class Builder
        {
            // Required parameters
            private readonly string name;
            private readonly bool resetOnReporting;

            // Optional parameters
            private string context;
            private Unit measurementUnit;
            private bool? reportItemPercentages;
            private bool? reportSetItems;
            private MetricTags tags;

            public Builder(string name)
            {
                this.name = name;

                // In order for delta values to be calculated correctly, the value must be reset
                // every time the delta counter is reported.
                resetOnReporting = true;

                // Set the default measurement unit, since it cannot be null
                measurementUnit = Unit.None;
            }

            /// <summary>
            ///     Sets the context for the delta counter.
            /// </summary>
            /// <returns><see cref="this"/></returns>
            /// <param name="context">The context.</param>
            public Builder Context(string context)
            {
                this.context = context;
                return this;
            }

            /// <summary>
            ///     Sets the measurement unit for the delta counter.
            /// </summary>
            /// <returns><see cref="this"/></returns>
            /// <param name="measurementUnit">The measurement unit.</param>
            public Builder MeasurementUnit(Unit measurementUnit)
            {
                this.measurementUnit = measurementUnit;
                return this;
            }

            /// <summary>
            ///     Sets whether or not to report percentages for set items of this delta counter.
            /// </summary>
            /// <returns><see cref="this"/></returns>
            /// <param name="reportItemPercentages">
            ///     If <c>true</c>, report percentages. If <c>false</c>, do not report percentages.
            /// </param>
            public Builder ReportItemPercentages(bool reportItemPercentages)
            {
                this.reportItemPercentages = reportItemPercentages;
                return this;
            }

            /// <summary>
            ///     Sets whether or not to report set items of this delta counter.
            /// </summary>
            /// <returns><see cref="this"/></returns>
            /// <param name="reportSetItems">
            ///     If <c>true</c>, report set items. If <c>false</c>, do not report set items.
            /// </param>
            public Builder ReportSetItems(bool reportSetItems)
            {
                this.reportSetItems = reportSetItems;
                return this;
            }

            /// <summary>
            ///     Sets the metric tags for the delta counter.
            /// </summary>
            /// <returns><see cref="this"/></returns>
            /// <param name="tags">The metric tags.</param>
            public Builder Tags(MetricTags tags)
            {
                this.tags = tags;
                return this;
            }

            /// <summary>
            ///     Creates a new <see cref="DeltaCounterOptions"/> instance. In order for the
            ///     delta counter to be reported correctly, the fields for the new instance
            ///     should not be changed after the instance is created.
            /// </summary>
            /// <returns>A new <see cref="DeltaCounterOptions"/></returns>
            public DeltaCounterOptions Build()
            {
                var options = new DeltaCounterOptions()
                {
                    Name = name,
                    ResetOnReporting = resetOnReporting
                };

                if (context != null)
                {
                    options.Context = context;
                }

                options.MeasurementUnit = measurementUnit;

                if (reportItemPercentages.HasValue)
                {
                    options.ReportItemPercentages = reportItemPercentages.Value;
                }
                if (reportSetItems.HasValue)
                {
                    options.ReportSetItems = reportSetItems.Value;
                }

                options.Tags = MetricTags.Concat(tags, new MetricTags(
                    WavefrontConstants.WavefrontMetricTypeTagKey, WavefrontMetricTypeTagValue));

                return options;
            }
        }

        private DeltaCounterOptions()
        {
        }

        /// <summary>
        ///     Determines whether or not a metric is in the format of a delta counter by
        ///     looking for a particular metric tag.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the tags identify a delta counter, <c>false</c> otherwise.
        /// </returns>
        /// <param name="tags">The metric tags.</param>
        public static bool IsDeltaCounter(MetricTags tags)
        {
            return WavefrontConstants.IsWavefrontMetricType(tags, WavefrontMetricTypeTagValue);

        }
    }
}
