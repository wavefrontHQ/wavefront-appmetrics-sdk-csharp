# Wavefront App Metrics Reporter

This package provides support for reporting metrics recorded by App Metrics to Wavefront via proxy or direct ingestion.

## Dependencies
  * .NET Standard (>= 2.0)
  * App.Metrics (>= 2.0.0)
  * Wavefront.SDK.CSharp (>= 0.1.0) ([Github repo](https://github.com/wavefrontHQ/wavefront-sdk-csharp/tree/han/refactoring-and-aspnetcore-updates))

## Usage

### Instantiate a WavefrontSender (either WavefrontProxyClient or WavefrontDirectIngestionClient)
See the [Wavefront sender documentation](https://github.com/wavefrontHQ/wavefront-sdk-csharp/blob/han/refactoring-and-aspnetcore-updates/README.md#usage) for details on how to instantiate WavefrontProxyClient or WavefrontDirectIngestionClient.

### Option 1: Enable the Wavefront reporter using Report.ToWavefront(wavefrontSender)
```csharp
  /*
   * Using the wavefrontSender instantiated by following the above instructions,
   * enable reporting of metrics to Wavefront.
   */
  var metrics = new MetricsBuilder()
    .Report.ToWavefront(wavefrontSender)
    .Build();
```

### Option 2: Configure and enable the Wavefront reporter using Report.ToWavefront(options)
```csharp
  /*
   * Using the wavefrontSender instantiated by following the above instructions,
   * enable reporting of metrics to Wavefront and set the source for your metrics. 
   */
  var metrics = new MetricsBuilder()
    .Report.ToWavefront(
      options => {
        options.WavefrontSender = wavefrontDirectIngestionClient;
        options.Source = "appServer1";
        options.WavefrontHistogram.ReportMinuteDistribution = true;
      })
    .Build();
```

### Configuration
The Wavefront reporter has the following configuration options:
  * WavefrontSender - the client that handles sending of metrics to Wavefront via proxy or direct ingestion.
  * Source - the source for your metrics.
  * WavefrontHistogram.ReportMinuteDistribution - report Wavefront Histograms aggregated into minute intervals.
  * WavefrontHistogram.ReportHourDistribution - report Wavefront Histograms aggregated into hour intervals.
  * WavefrontHistogram.ReportDayDistribution - report Wavefront Histograms aggregated into day intervals.
  * Filter - the filter used to filter metrics just for this reporter.
  * FlushInterval - the delay between reporting metrics.

### Running the reporter
If you have an ASP.NET Core application, refer to this page
(https://www.app-metrics.io/web-monitoring/aspnet-core/reporting/)
for instructions on how to schedule reporting.

Otherwise, you can run all configured reports using the `ReportRunner` on `IMetricsRoot`:

```csharp
  await metrics.ReportRunner.RunAllAsync();
```

Or you can use the `AppMetricsTaskScheduler` to schedule the reporting of metrics:

```csharp
  var scheduler = new AppMetricsTaskScheduler(
    TimeSpan.FromSeconds(10),
    async () =>
    {
      await Task.WhenAll(metrics.ReportRunner.RunAllAsync());
    });
  scheduler.Start();
```

### Wavefront-specific entities that you can report
```csharp
  /* 
   * Wavefront Delta Counter
   * 
   * Configure and instantiate using DeltaCounterOptions.Builder.
   * Do not update option fields after instantiation.
   */
  var myDeltaCounter = new DeltaCounterOptions
    .Builder("myDeltaCounter")
    .MeasurementUnit(Unit.Calls)
    .Tags(new MetricTags("cluster", "us-west"))
    .Build();
    
  // Increment the counter by 1
  metrics.Measure.Counter.Increment(myDeltaCounter);
  
  // Increment the counter by n
  metrics.Measure.Counter.Increment(myDeltaCounter, n);
  
  
  /* 
   * Wavefront Histogram
   * 
   * Configure and instantiate using WavefrontHistogramOptions.Builder.
   * Do not update option fields after instantiation.
   */
  var myWavefrontHistogram = new WavefrontHistogramOptions
    .Builder("myWavefrontHistogram")
    .MeasurementUnit(Unit.KiloBytes)
    .Tags(new MetricTags("cluster", "us-west"))
    .Build();
    
  // Add a value to the histogram
  metrics.Measure.Histogram.Update(myWavefrontHistogram, myValue);

```
