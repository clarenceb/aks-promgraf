﻿namespace Prometheus;

internal sealed class LabelEnrichingManagedLifetimeHistogram : IManagedLifetimeMetricHandle<IHistogram>
{
    public LabelEnrichingManagedLifetimeHistogram(IManagedLifetimeMetricHandle<IHistogram> inner, string[] enrichWithLabelValues)
    {
        _inner = inner;
        _enrichWithLabelValues = enrichWithLabelValues;
    }

    private readonly IManagedLifetimeMetricHandle<IHistogram> _inner;
    private readonly string[] _enrichWithLabelValues;

    public IDisposable AcquireLease(out IHistogram metric, params string[] labelValues)
    {
        return _inner.AcquireLease(out metric, WithEnrichedLabelValues(labelValues));
    }

    public ICollector<IHistogram> WithExtendLifetimeOnUse()
    {
        return new LabelEnrichingAutoLeasingMetric<IHistogram>(_inner.WithExtendLifetimeOnUse(), _enrichWithLabelValues);
    }

    public void WithLease(Action<IHistogram> action, params string[] labelValues)
    {
        _inner.WithLease(action, WithEnrichedLabelValues(labelValues));
    }

    public TResult WithLease<TResult>(Func<IHistogram, TResult> func, params string[] labelValues)
    {
        return _inner.WithLease(func, WithEnrichedLabelValues(labelValues));
    }

    public Task WithLeaseAsync(Func<IHistogram, Task> func, params string[] labelValues)
    {
        return _inner.WithLeaseAsync(func, WithEnrichedLabelValues(labelValues));
    }

    public Task<TResult> WithLeaseAsync<TResult>(Func<IHistogram, Task<TResult>> action, params string[] labelValues)
    {
        return _inner.WithLeaseAsync(action, WithEnrichedLabelValues(labelValues));
    }

    private string[] WithEnrichedLabelValues(string[] instanceLabelValues)
    {
        return _enrichWithLabelValues.Concat(instanceLabelValues).ToArray();
    }
}
