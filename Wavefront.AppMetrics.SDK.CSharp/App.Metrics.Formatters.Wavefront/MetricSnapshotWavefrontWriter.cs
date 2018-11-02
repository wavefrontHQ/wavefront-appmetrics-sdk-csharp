using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Histogram;
using App.Metrics.Serialization;
using Wavefront.SDK.CSharp.Common;
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
        private static readonly Regex SimpleNames = new Regex("[^a-zA-Z0-9_.\\-~]");
        private static readonly HashSet<string> TagsToExclude =
            new HashSet<string> { WavefrontConstants.WavefrontMetricTypeTagKey };

        private readonly IWavefrontSender wavefrontSender;
        private readonly string source;
        private readonly ISet<HistogramGranularity> histogramGranularities;

        public MetricSnapshotWavefrontWriter(
            IWavefrontSender wavefrontSender,
            string source,
            ISet<HistogramGranularity> histogramGranularities)
        {
            this.wavefrontSender = wavefrontSender;
            this.source = source;
            this.histogramGranularities = histogramGranularities;

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

            string metricTypeValue = tags.Values[Array.IndexOf(tags.Keys, Pack.MetricTagsTypeKey)];
            var fields = columns.Zip(values, (column, data) => new { column, data })
                                .ToDictionary(pair => pair.column, pair => pair.data);

            if (metricTypeValue == Pack.ApdexMetricTypeValue)
            {
                WriteApdex(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.CounterMetricTypeValue)
            {
                WriteCounter(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.GaugeMetricTypeValue)
            {
                WriteGauge(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.HistogramMetricTypeValue)
            {
                WriteHistogram(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.MeterMetricTypeValue)
            {
                WriteMeter(context, name, fields, tags, timestamp);
            }
            else if (metricTypeValue == Pack.TimerMetricTypeValue)
            {
                WriteMeter(context, name, fields, tags, timestamp);
                WriteHistogram(context, name, fields, tags, timestamp);
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
                            ConcatAndSanitize(context, name, suffix),
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
                name = ConcatAndSanitize(context, name);

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
            wavefrontSender.SendMetric(ConcatAndSanitize(context, name, suffix),
                                       Convert.ToDouble(value),
                                       UnixTime(timestamp),
                                       source,
                                       FilterTags(tags)
                                      );
        }

        private string ConcatAndSanitize(params string[] components)
        {
            return Sanitize(String.Join(".", components));
        }

        private string Sanitize(string name)
        {
            return SimpleNames.Replace(name, "_");
        }

        private long UnixTime(DateTime timestamp)
        {
            return new DateTimeOffset(timestamp).ToUnixTimeSeconds();
        }

        private Dictionary<string, string> FilterTags(MetricTags tags)
        {
            return tags.ToDictionary().Where(tag => !TagsToExclude.Contains(tag.Key))
                       .ToDictionary(tag => tag.Key, tag => tag.Value);
        }

        public void Dispose()
        {
        }
    }
}
