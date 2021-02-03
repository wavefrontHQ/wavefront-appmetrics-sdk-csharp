using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Histogram;
using App.Metrics.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wavefront.SDK.CSharp.Common;
using Wavefront.SDK.CSharp.Common.Metrics;
using Wavefront.SDK.CSharp.Entities.Histograms;
using Wavefront.SDK.CSharp.Entities.Metrics;
using static App.Metrics.AppMetricsConstants;

namespace App.Metrics.Formatters.Wavefront
{
    /// <summary>
    /// Handles the writing of a metrics snapshot to a Wavefront sender.
    /// </summary>
    public class MetricSnapshotWavefrontWriter : IMetricSnapshotWriter
    {
        private static readonly HashSet<string> TagsToExclude =
            new HashSet<string> { WavefrontConstants.WavefrontMetricTypeTagKey };

        private readonly IWavefrontSender wavefrontSender;
        private readonly string source;
        private readonly IDictionary<string, string> globalTags;
        private readonly ISet<HistogramGranularity> histogramGranularities;
        private readonly MetricFields fields;

        private readonly WavefrontSdkDeltaCounter gaugesReported;
        private readonly WavefrontSdkDeltaCounter deltaCountersReported;
        private readonly WavefrontSdkDeltaCounter countersReported;
        private readonly WavefrontSdkDeltaCounter wfHistogramsReported;
        private readonly WavefrontSdkDeltaCounter histogramsReported;
        private readonly WavefrontSdkDeltaCounter metersReported;
        private readonly WavefrontSdkDeltaCounter timersReported;
        private readonly WavefrontSdkDeltaCounter apdexesReported;
        private readonly WavefrontSdkDeltaCounter writerErrors;

        public MetricSnapshotWavefrontWriter(
            IWavefrontSender wavefrontSender,
            string source,
            IDictionary<string, string> globalTags,
            ISet<HistogramGranularity> histogramGranularities,
            WavefrontSdkMetricsRegistry sdkMetricsRegistry,
            MetricFields fields)
        {
            this.wavefrontSender = wavefrontSender;
            this.source = source;
            this.globalTags = globalTags;
            this.histogramGranularities = histogramGranularities;
            this.fields = fields;

            gaugesReported = sdkMetricsRegistry.DeltaCounter("gauges.reported");
            deltaCountersReported = sdkMetricsRegistry.DeltaCounter("delta_counters.reported");
            countersReported = sdkMetricsRegistry.DeltaCounter("counters.reported");
            wfHistogramsReported = sdkMetricsRegistry.DeltaCounter("wavefront_histograms.reported");
            histogramsReported = sdkMetricsRegistry.DeltaCounter("histograms.reported");
            metersReported = sdkMetricsRegistry.DeltaCounter("meters.reported");
            timersReported = sdkMetricsRegistry.DeltaCounter("timers.reported");
            apdexesReported = sdkMetricsRegistry.DeltaCounter("apdexes.reported");
            writerErrors = sdkMetricsRegistry.DeltaCounter("writer.errors");
        }

        public void Write(string context, string name, string field, object value,
                          MetricTags tags, DateTime timestamp)
        {
            Write(context, name, new[] { field }, new[] { value }, tags, timestamp);
        }

        /// <inheritdoc />
        public void Write(string context, string name, IEnumerable<string> columns,
                          IEnumerable<object> values, MetricTags tags, DateTime timestamp)
        {
            // Do not report App Metrics' internal metrics (e.g., report_success counter)
            if (context == InternalMetricsContext)
            {
                return;
            }

            try
            {
                string metricTypeValue =
                    tags.Values[Array.IndexOf(tags.Keys, Pack.MetricTagsTypeKey)];
                var data = columns.Zip(values, (column, value) => new { column, value })
                                    .ToDictionary(pair => pair.column, pair => pair.value);

                if (metricTypeValue == Pack.ApdexMetricTypeValue)
                {
                    WriteApdex(context, name, data, tags, timestamp);
                    apdexesReported.Inc();
                }
                else if (metricTypeValue == Pack.CounterMetricTypeValue)
                {
                    WriteCounter(context, name, data, tags, timestamp);
                    if (DeltaCounterOptions.IsDeltaCounter(tags))
                    {
                        deltaCountersReported.Inc();
                    }
                    else
                    {
                        countersReported.Inc();
                    }
                }
                else if (metricTypeValue == Pack.GaugeMetricTypeValue)
                {
                    WriteGauge(context, name, data, tags, timestamp);
                    gaugesReported.Inc();
                }
                else if (metricTypeValue == Pack.HistogramMetricTypeValue)
                {
                    WriteHistogram(context, name, data, tags, timestamp);
                    if (WavefrontHistogramOptions.IsWavefrontHistogram(tags))
                    {
                        wfHistogramsReported.Inc();
                    }
                    else
                    {
                        histogramsReported.Inc();
                    }
                }
                else if (metricTypeValue == Pack.MeterMetricTypeValue)
                {
                    WriteMeter(context, name, data, tags, timestamp);
                    metersReported.Inc();
                }
                else if (metricTypeValue == Pack.TimerMetricTypeValue)
                {
                    WriteMeter(context, name, data, tags, timestamp);
                    WriteHistogram(context, name, data, tags, timestamp);
                    timersReported.Inc();
                }
            }
            catch (Exception e)
            {
                writerErrors.Inc();
                throw e;
            }
        }

        private void WriteApdex(string context, string name, IDictionary<string, object> data,
                                MetricTags tags, DateTime timestamp)
        {
            foreach (var field in fields.Apdex)
            {
                if (data.ContainsKey(field.Value))
                {
                    WriteInternal(context, name, field.Value, data[field.Value], tags, timestamp);
                }
            }
        }

        private void WriteCounter(string context, string name, IDictionary<string, object> data,
                                  MetricTags tags, DateTime timestamp)
        {
            bool isDeltaCounter = DeltaCounterOptions.IsDeltaCounter(tags);

            foreach (var field in fields.Counter)
            {
                if (data.ContainsKey(field.Value))
                {
                    var suffix = field.Value.Equals("value") ? "count" : field.Value;
                    // Report delta counters using an API that is specific to delta counters.
                    if (isDeltaCounter)
                    {
                        wavefrontSender.SendDeltaCounter(
                            Concat(context, name, suffix),
                            Convert.ToDouble(data[field.Value]),
                            source,
                            FilterTags(tags)
                        );

                    }
                    else
                    {
                        WriteInternal(context, name, suffix, data[field.Value], tags, timestamp);
                    }
                }
            }
        }

        private void WriteGauge(string context, string name, IDictionary<string, object> data,
                                MetricTags tags, DateTime timestamp)
        {
            foreach (var field in fields.Gauge)
            {
                if (data.ContainsKey(field.Value))
                {
                    WriteInternal(context, name, field.Value, data[field.Value], tags, timestamp);
                }
            }
        }

        private void WriteHistogram(string context, string name, IDictionary<string, object> data,
                                    MetricTags tags, DateTime timestamp)
        {
            // Report Wavefront Histograms using an API that is specific to Wavefront Histograms.
            if (WavefrontHistogramOptions.IsWavefrontHistogram(tags))
            {
                name = Concat(context, name);

                // Wavefront Histograms are reported as a distribution, so we must extract the
                // distribution from a HistogramValue that is carrying it in a serialized format.
                string keyFieldName = fields.Histogram[HistogramFields.UserMaxValue];
                string valueFieldName = fields.Histogram[HistogramFields.UserMinValue];

                if (data.ContainsKey(keyFieldName) && data.ContainsKey(valueFieldName))
                {
                    string key = (string)data[keyFieldName];
                    string value = (string)data[valueFieldName];

                    // Deserialize the distributions into the right format for reporting.
                    var distributions = WavefrontHistogramImpl.Deserialize(
                        new KeyValuePair<string, string>(key, value));

                    foreach (var distribution in distributions)
                    {
                        wavefrontSender.SendDistribution(
                            name,
                            distribution.Centroids,
                            histogramGranularities,
                            distribution.Timestamp,
                            source,
                            FilterTags(tags)
                        );
                    }
                }
            }
            else
            {
                foreach (var field in fields.Histogram)
                {
                    // Do not report non-numerical metrics
                    if (field.Key == HistogramFields.UserLastValue ||
                        field.Key == HistogramFields.UserMaxValue ||
                        field.Key == HistogramFields.UserMinValue)
                    {
                        continue;
                    }

                    if (data.ContainsKey(field.Value))
                    {
                        WriteInternal(context, name, field.Value, data[field.Value], tags,
                            timestamp);
                    }
                }
            }
        }

        private void WriteMeter(string context, string name, IDictionary<string, object> data,
                                MetricTags tags, DateTime timestamp)
        {
            foreach (var field in fields.Meter)
            {
                if (data.ContainsKey(field.Value))
                {
                    WriteInternal(context, name, field.Value, data[field.Value], tags, timestamp);
                }
            }
        }

        private void WriteInternal(string context, string name, string suffix, object value,
                                   MetricTags tags, DateTime timestamp)
        {
            wavefrontSender.SendMetric(Concat(context, name, suffix),
                                       Convert.ToDouble(value),
                                       DateTimeUtils.UnixTimeMilliseconds(timestamp),
                                       source,
                                       FilterTags(tags)
                                      );
        }

        private string Concat(params string[] components)
        {
            // sanitization is handled by the Wavefront sender
            return string.Join(".", components);
        }

        /// <summary>
        /// Returns a dictionary of point and global tags that are to be sent to Wavefront.
        /// Visible for testing only.
        /// </summary>
        /// <param name="tags">Metric point tags.</param>
        /// <returns>A dictionary of all tags to be sent to Wavefront.</returns>
        internal Dictionary<string, string> FilterTags(MetricTags tags)
        {
            var tagsDict = globalTags == null ? new Dictionary<string, string>() :
                new Dictionary<string, string>(globalTags);
            for (int i = 0; i < tags.Keys.Length; i++)
            {
                string key = tags.Keys[i];
                if (!TagsToExclude.Contains(key))
                {
                    tagsDict[key] = tags.Values[i];
                }
            }
            return tagsDict;
        }

        public void Dispose()
        {
        }

#if !NET452
        public ValueTask DisposeAsync()
        {
            return default;
        }
#endif
    }
}
