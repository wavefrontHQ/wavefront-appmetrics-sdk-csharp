using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Histogram;
using App.Metrics.Serialization;
using Wavefront.CSharp.SDK.Common;
using Wavefront.CSharp.SDK.Entities.Histograms;
using Wavefront.CSharp.SDK.Entities.Metrics;
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
        private readonly MetricFields fields;

        public MetricSnapshotWavefrontWriter(
            IWavefrontSender wavefrontSender,
            string source,
            ISet<HistogramGranularity> histogramGranularities,
            MetricFields fields)
        {
            this.wavefrontSender = wavefrontSender;
            this.source = source;
            this.histogramGranularities = histogramGranularities;
            this.fields = fields;
        }

        /// <inheritdoc />
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

            string metricTypeValue = tags.Values[Array.IndexOf(tags.Keys, Pack.MetricTagsTypeKey)];
            var data = columns.Zip(values, (column, value) => new { column, value })
                              .ToDictionary(pair => pair.column, pair => pair.value);

            if (metricTypeValue == Pack.ApdexMetricTypeValue)
            {
                WriteApdex(context, name, data, tags, timestamp);
            }
            else if (metricTypeValue == Pack.CounterMetricTypeValue)
            {
                WriteCounter(context, name, data, tags, timestamp);
            }
            else if (metricTypeValue == Pack.GaugeMetricTypeValue)
            {
                WriteGauge(context, name, data, tags, timestamp);
            }
            else if (metricTypeValue == Pack.HistogramMetricTypeValue)
            {
                WriteHistogram(context, name, data, tags, timestamp);
            }
            else if (metricTypeValue == Pack.MeterMetricTypeValue)
            {
                WriteMeter(context, name, data, tags, timestamp);
            }
            else if (metricTypeValue == Pack.TimerMetricTypeValue)
            {
                WriteMeter(context, name, data, tags, timestamp);
                WriteHistogram(context, name, data, tags, timestamp);
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
                    // Report delta counters using an API that is specific to delta counters.
                    if (isDeltaCounter)
                    {
                        wavefrontSender.SendDeltaCounter(
                            ConcatAndSanitize(context, name, field.Value),
                            Convert.ToDouble(data[field.Value]),
                            source,
                            FilterTags(tags)
                        );

                    }
                    else
                    {
                        WriteInternal(context, name, field.Value, data[field.Value], tags,
                                      timestamp);
                    }
                }
            }
        }

        private void WriteGauge(string context, string name, IDictionary<string, object> data,
                                MetricTags tags, DateTime timestamp)
        {
            WriteInternal(context, name, "value", data["value"], tags, timestamp);
        }

        private void WriteHistogram(string context, string name, IDictionary<string, object> data,
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
                    fields.Histogram[HistogramFields.UserMaxValue];
                string valueFieldName =
                    fields.Histogram[HistogramFields.UserMinValue];

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

        private void WriteInternal(string context, string name, string subname, object value,
                           MetricTags tags, DateTime timestamp)
        {
            wavefrontSender.SendMetric(ConcatAndSanitize(context, name, subname),
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
