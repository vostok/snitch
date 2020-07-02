using System;
using System.Collections.Generic;
using Vostok.Metrics.Models;

namespace Vostok.Snitch.Metrics
{
    internal class MetricsTagsCache
    {
        private const int Limit = 1_000_000;
        private readonly Func<string, MetricTags> get1;
        private readonly Func<string, string, MetricTags> get2;

        private readonly MetricTags cache0;
        private readonly Dictionary<string, MetricTags> cache1 = new Dictionary<string, MetricTags>();
        private readonly Dictionary<(string, string), MetricTags> cache2 = new Dictionary<(string, string), MetricTags>();

        public MetricsTagsCache(Func<MetricTags> get) =>
            cache0 = get();

        public MetricsTagsCache(Func<string, MetricTags> get) =>
            get1 = get;

        public MetricsTagsCache(Func<string, string, MetricTags> get) =>
            get2 = get;

        public MetricTags For() =>
            cache0;

        public MetricTags For(string value)
        {
            value = value ?? "unknown";

            if (cache1.TryGetValue(value, out var result))
                return result;

            result = get1(value);

            if (cache1.Count > Limit)
                cache1.Clear();

            cache1.Add(value, result);

            return result;
        }

        public MetricTags For(string value1, string value2)
        {
            value1 = value1 ?? "unknown";
            value2 = value2 ?? "unknown";

            if (cache2.TryGetValue((value1, value2), out var result))
                return result;

            result = get2(value1, value2);

            if (cache2.Count > Limit)
                cache2.Clear();

            cache2.Add((value1, value2), result);

            return result;
        }
    }
}