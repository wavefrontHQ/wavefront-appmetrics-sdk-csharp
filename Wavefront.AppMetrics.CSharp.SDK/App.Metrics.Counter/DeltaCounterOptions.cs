using System;
using Wavefront.CSharp.SDK.Common;

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
                // The Wavefront reporter identifies delta counters by a prefix on the metric name.
                this.name = AddPrefix(name);

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
            ///     delta counter to be reported correctly, the fields the new instance
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

                options.Tags = tags;

                return options;
            }
        }

        private DeltaCounterOptions()
        {
        }

        /// <summary>
        ///     Determines whether or not a metric name is in the format of a delta counter.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the name identifies a delta counter, <c>false</c> otherwise.
        /// </returns>
        /// <param name="name">The metric name.</param>
        public static bool IsDeltaCounter(string name)
        {
            return name.StartsWith(Constants.DeltaPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Adds the delta counter prefix to a metric name, allowing the metric to be
        ///     identified as a delta counter.
        /// </summary>
        /// <returns>The prefixed name.</returns>
        /// <param name="name">The metric name.</param>
        public static string AddPrefix(string name)
        {
            return Constants.DeltaPrefix + name;
        }

        /// <summary>
        ///     Removes the delta counter prefix from a metric name.
        /// </summary>
        /// <returns>The original name of the metric.</returns>
        /// <param name="name">The delta counter's name.</param>
        public static string RemovePrefix(string name)
        {
            return name.Substring(Constants.DeltaPrefix.Length);
        }
    }
}
