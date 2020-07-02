using System;

namespace Vostok.Snitch.Metrics.Models
{
    public class ReplicaWeight
    {
        public readonly DateTimeOffset Timestamp;

        public readonly double Value;

        public ReplicaWeight(DateTimeOffset timestamp, double value)
        {
            Timestamp = timestamp;
            Value = value;
        }

        public ReplicaWeight WithValue(double value) =>
            new ReplicaWeight(Timestamp, value);

        public override string ToString() =>
            $"{nameof(Timestamp)}: {Timestamp}, {nameof(Value)}: {Value:F2}";
    }
}