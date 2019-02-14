# Wavefront App Metrics Reporter [![travis build status](https://travis-ci.com/wavefrontHQ/wavefront-appmetrics-sdk-csharp.svg?branch=master)](https://travis-ci.com/wavefrontHQ/wavefront-appmetrics-sdk-csharp) [![NuGet](https://img.shields.io/nuget/v/Wavefront.AppMetrics.SDK.CSharp.svg)](https://www.nuget.org/packages/Wavefront.AppMetrics.SDK.CSharp)

This package provides support for reporting metrics recorded by App Metrics to Wavefront via proxy or direct ingestion.

## Frameworks Supported
  * .NET Framework (>= 4.5.2)
  * .NET Standard (>= 2.0)

## Installation
Install the [NuGet package](https://www.nuget.org/packages/Wavefront.AppMetrics.SDK.CSharp/).

### Package Manager Console

```
PM> Install-Package Wavefront.AppMetrics.SDK.CSharp
```

### .NET CLI Console

```
> dotnet add package Wavefront.AppMetrics.SDK.CSharp
```

## Set Up App Metrics with Wavefront
This SDK adds Wavefront integrations to App Metrics, allowing for the reporting of metrics and histograms to Wavefront.

The steps for creating an App Metrics `IMetrics` instance that reports to Wavefront are:
1. Create a `MetricsBuilder` instance.
2. Create an `IWavefrontSender` instance for sending data to Wavefront.
3. Use the `MetricsBuilder` to configure reporting to Wavefront using the `IWavefrontSender`.
4. Use the `MetricsBuilder` to build an `IMetrics` instance.

For the details of each step, see the sections below.

### 1. Create a Builder for IMetrics
An App Metrics `IMetrics` object serves as an interface for storing and reporting metrics and histograms. We create a builder to configure and build an `IMetrics` instance.

```csharp
// Create a builder instance for App Metrics
var metricsBuilder = new MetricsBuilder();
```

You can optionally configure `IMetrics` using configuration options that can be accessed via the builder. See [App Metrics documentation](https://www.app-metrics.io/getting-started/fundamentals/configuration/) on this subject for more details.

### 2. Set Up an IWavefrontSender
An `IWavefrontSender` object implements the low-level interface for sending data to Wavefront. You can choose to send data to Wavefront using either the [Wavefront proxy](https://docs.wavefront.com/proxies.html) or [direct ingestion](https://docs.wavefront.com/direct_ingestion.html).

* See [Set Up an IWavefrontSender](https://github.com/wavefrontHQ/wavefront-sdk-csharp/blob/master/README.md#set-up-an-iwavefrontsender) for details on instantiating a proxy or direct ingestion client.

**Note:** If you are using multiple Wavefront C# SDKs, see [Sharing an IWavefrontSender](https://github.com/wavefrontHQ/wavefront-sdk-csharp/blob/master/docs/sender.md) for information about sharing a single `IWavefrontSender` instance across SDKs.

### 3. Configure Reporting to Wavefront
To enable reporting of metrics and histograms to Wavefront, you must configure the `MetricsBuilder` to use the `IWavefrontSender`.

```csharp
// Configure the builder instance to report to Wavefront
metricsBuilder.Report.ToWavefront(
  options =>
  {
    options.WavefrontSender = BuildWavefrontSender(); // pseudocode; see above
    options.Source = "appServer1"; // optional
    options.WavefrontHistogram.ReportMinuteDistribution = true; // optional
  });
```

The Wavefront reporter has the following configuration options:

|Property|Description|Required?|
|-------------|-------------|:-----:|
|WavefrontSender|The IWavefrontSender instance that handles sending of data to Wavefront.|Y|
|Source|The source of your metrics. Defaults to your local host name.|N|
|WavefrontHistogram.ReportMinuteDistribution|Whether to report Wavefront Histograms aggregated into minute intervals. Defaults to false.|N|
|WavefrontHistogram.ReportHourDistribution|Whether to report Wavefront Histograms aggregated into hour intervals. Defaults to false.|N|
|WavefrontHistogram.ReportDayDistribution|Whether to report Wavefront Histograms aggregated into day intervals. Defaults to false.|N|
|Filter|The IFilterMetrics that will be used by this reporter for filtering metrics.|N|
|FlushInterval|The interval between flushing metrics.|N|
|ApplicationTags|Metadata about your application that will be reported to Wavefront as point tags.|N|

### 4. Build an IMetrics Instance
After configuring the builder to report to Wavefront, we're ready to build our `IMetrics` instance.

```csharp
var metrics = metricsBuilder.Build();
```

## Running the Reporter
If you have an ASP.NET Core application, refer to [these instructions](https://www.app-metrics.io/web-monitoring/aspnet-core/reporting/) on how to schedule reporting.

Alternatively, you can manually run your configured reporter(s) using the `ReportRunner` in your `IMetrics` instance:

```csharp
await metrics.ReportRunner.RunAllAsync();
```

Or you can use the `AppMetricsTaskScheduler` to schedule reporting:

```csharp
var scheduler = new AppMetricsTaskScheduler(
  TimeSpan.FromSeconds(10),
  async () =>
  {
    await Task.WhenAll(metrics.ReportRunner.RunAllAsync());
  });
scheduler.Start();
```

See [App Metrics documentation on reporting](https://www.app-metrics.io/getting-started/#reporting-metrics) for more details.

## Types of Data You Can Report to Wavefront
App Metrics supports various [metric types](https://www.app-metrics.io/getting-started/metric-types/). This Wavefront SDK additionally provides a [`DeltaCounter`](https://docs.wavefront.com/delta_counters.html) type and a [`WavefrontHistogram`](https://docs.wavefront.com/proxies_histograms.html) type.

To create and start reporting a `DeltaCounter`:

```csharp
// Configure and instantiate a DeltaCounter using DeltaCounterOptions.Builder.
var myDeltaCounter = new DeltaCounterOptions.Builder("myDeltaCounter")
  .MeasurementUnit(Unit.Calls)
  .Tags(new MetricTags("cluster", "us-west"))
  .Build();

// Increment the counter by 1
metrics.Measure.Counter.Increment(myDeltaCounter);

// Increment the counter by n
metrics.Measure.Counter.Increment(myDeltaCounter, n);
```

To create and start reporting a `WavefrontHistogram`:

```csharp
// Configure and instantiate a WavefrontHistogram using WavefrontHistogramOptions.Builder.
var myWavefrontHistogram = new WavefrontHistogramOptions.Builder("myWavefrontHistogram")
  .MeasurementUnit(Unit.KiloBytes)
  .Tags(new MetricTags("cluster", "us-west"))
  .Build();

// Add a value to the histogram
metrics.Measure.Histogram.Update(myWavefrontHistogram, myValue);

```
