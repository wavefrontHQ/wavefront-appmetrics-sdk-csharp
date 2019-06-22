using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Histogram;
using App.Metrics.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly WavefrontSdkCounter gaugesReported;
        private readonly WavefrontSdkCounter deltaCountersReported;
        private readonly WavefrontSdkCounter countersReported;
        private readonly WavefrontSdkCounter wfHistogramsReported;
        private readonly WavefrontSdkCounter histogramsReported;
        private readonly WavefrontSdkCounter metersReported;
        private readonly WavefrontSdkCounter timersReported;
        private readonly WavefrontSdkCounter apdexesReported;
        private readonly WavefrontSdkCounter writerErrors;

        public MetricSnapshotWavefrontWriter(
            IWavefrontSender wavefrontSender,
            string source,
            IDictionary<string, string> globalTags,
            ISet<HistogramGranularity> histogramGranularities,
            WavefrontSdkMetricsRegistry sdkMetricsRegistry)
        {
            this.wavefrontSender = wavefrontSender;
            this.source = source;
            this.globalTags = globalTags;
            this.histogramGranularities = histogramGranularities;

            gaugesReported = sdkMetricsRegistry.Counter("gauges.reported");
            deltaCountersReported = sdkMetricsRegistry.Counter("delta_counters.reported");
            countersReported = sdkMetricsRegistry.Counter("counters.reported");
            wfHistogramsReported = sdkMetricsRegistry.Counter("wavefront_histograms.reported");
            histogramsReported = sdkMetricsRegistry.Counter("histograms.reported");
            metersReported = sdkMetricsRegistry.Counter("meters.reported");
            timersReported = sdkMetricsRegistry.Counter("timers.reported");
            apdexesReported = sdkMetricsRegistry.Counter("apdexes.reported");
            writerErrors = sdkMetricsRegistry.Counter("writer.errors");

            MetricNameMapping = new GeneratedMetricNameMapping();
        }

        /// <inheritdoc />
        public GeneratedMetricNameMapping MetricNameMapping { get; } =
            new GeneratedMetricNameMapping();

        public void Write(string context, string name, object value,
                          MetricTags tags, DateTime timestamp)
        {
            Write(context, name, new[] { "value" }, new[] { value }, tags, timestamp);
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
                var fields = columns.Zip(values, (column, data) => new { column, data })
                                    .ToDictionary(pair => pair.column, pair => pair.data);

                if (metricTypeValue == Pack.ApdexMetricTypeValue)
                {
                    WriteApdex(context, name, fields, tags, timestamp);
                    apdexesReported.Inc();
                }
                else if (metricTypeValue == Pack.CounterMetricTypeValue)
                {
                    WriteCounter(context, name, fields, tags, timestamp);
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
                    WriteGauge(context, name, fields, tags, timestamp);
                    gaugesReported.Inc();
                }
                else if (metricTypeValue == Pack.HistogramMetricTypeValue)
                {
                    WriteHistogram(context, name, fields, tags, timestamp);
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
                    WriteMeter(context, name, fields, tags, timestamp);
                    metersReported.Inc();
                }
                else if (metricTypeValue == Pack.TimerMetricTypeValue)
                {
                    WriteMeter(context, name, fields, tags, timestamp);
                    WriteHistogram(context, name, fields, tags, timestamp);
                    timersReported.Inc();
                }
            }
            catch (Exception e)
            {
                writerErrors.Inc();
                throw e;
            }
        }

        private void WriteApdex(string context, string name, IDictionary<string, object> fields,
                                MetricTags tags, DateTime timestamp)
        {
            foreach (var entry in MetricNameMapping.Apdex)
            {
                if (fields.ContainsKey(entry.Value))
                {
                    Write(context, name, entry.Value, fields[entry.Value], tags, timestamp);
                }
            }
        }

        private void WriteCounter(string context, string name, IDictionary<string, object> fields,
                                  MetricTags tags, DateTime timestamp)
        {
            bool isDeltaCounter = DeltaCounterOptions.IsDeltaCounter(tags);

            var metricNames = MetricNameMapping.Counter.Values.Union(new string[] {"value"});

            foreach (string metricName in metricNames)
            {
                if (fields.ContainsKey(metricName))
                {
                    var suffix = metricName.Equals("value") ? "count" : metricName;
                    // Report delta counters using an API that is specific to delta counters.
                    if (isDeltaCounter)
                    {
                        wavefrontSender.SendDeltaCounter(
                            Concat(context, name, suffix),
                            Convert.ToDouble(fields[metricName]),
                            source,
                            FilterTags(tags)
                        );

                    }
                    else
                    {
                        Write(context, name, suffix, fields[metricName], tags, timestamp);
                    }
                }
            }
        }

        private void WriteGauge(string context, string name, IDictionary<string, object> fields,
                                MetricTags tags, DateTime timestamp)
        {
            Write(context, name, "value", fields["value"], tags, timestamp);
        }

        private void WriteHistogram(string context, string name, IDictionary<string, object> fields,
                                    MetricTags tags, DateTime timestamp)
        {
            bool isWavefrontHistogram = WavefrontHistogramOptions.IsWavefrontHistogram(tags);

            // Report Wavefront Histograms using an API that is specific to Wavefront Histograms.
            if (isWavefrontHistogram)
            {
                name = Concat(context, name);

                // Wavefront Histograms are reported as a distribution, so we must extract the
                // distribution from a HistogramValue that is carrying it in a serialized format.
                string keyFieldName =
                    MetricNameMapping.Histogram[HistogramValueDataKeys.UserMaxValue];
                string valueFieldName =
                    MetricNameMapping.Histogram[HistogramValueDataKeys.UserMinValue];

                if (fields.ContainsKey(keyFieldName) && fields.ContainsKey(valueFieldName))
                {
                    string key = (string)fields[keyFieldName];
                    string value = (string)fields[valueFieldName];

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
                foreach (var entry in MetricNameMapping.Histogram)
                {
                    // Do not report non-numerical metrics
                    if (entry.Key == HistogramValueDataKeys.UserLastValue ||
                        entry.Key == HistogramValueDataKeys.UserMaxValue ||
                        entry.Key == HistogramValueDataKeys.UserMinValue)
                    {
                        continue;
                    }

                    if (fields.ContainsKey(entry.Value))
                    {
                        Write(context, name, entry.Value, fields[entry.Value], tags, timestamp);
                    }
                }
            }
        }

        private void WriteMeter(string context, string name, IDictionary<string, object> fields,
                                MetricTags tags, DateTime timestamp)
        {
            foreach (var entry in MetricNameMapping.Meter)
            {
                if (fields.ContainsKey(entry.Value))
                {
                    Write(context, name, entry.Value, fields[entry.Value], tags, timestamp);
                }
            }
        }

        private void Write(string context, string name, string suffix, object value,
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

        private Dictionary<string, string> FilterTags(MetricTags tags)
        {
            var tagsDict = globalTags == null ? new Dictionary<string, string>() :
                new Dictionary<string, string>(globalTags);
            foreach (var tag in tags.ToDictionary().Where(tag => !TagsToExclude.Contains(tag.Key)))
            {
                tagsDict[tag.Key] = tag.Value;
            }
            return tagsDict;
        }

        public void Dispose()
        {
        }
    }
}
