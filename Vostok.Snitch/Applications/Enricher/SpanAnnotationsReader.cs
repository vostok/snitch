using Vostok.Clusterclient.Core.Model;
using Vostok.Hercules.Client.Abstractions.Events;
using Vostok.Tracing.Abstractions;

namespace Vostok.Snitch.Applications.Enricher
{
    internal class SpanAnnotationsReader : DummyHerculesTagsBuilder, IHerculesTagsBuilder
    {
        public string Kind;
        public string Url;
        public string Method;
        public string TargetEnvironment;
        public string TargetService;
        public ResponseCode? Code;

        public new IHerculesTagsBuilder AddValue(string key, string value)
        {
            switch (key)
            {
                case WellKnownAnnotations.Common.Kind:
                    Kind = value;
                    break;
                case WellKnownAnnotations.Http.Request.Url:
                    Url = value;
                    break;
                case WellKnownAnnotations.Http.Request.TargetEnvironment:
                    TargetEnvironment = value;
                    break;
                case WellKnownAnnotations.Http.Request.TargetService:
                    TargetService = value;
                    break;
                case WellKnownAnnotations.Http.Request.Method:
                    Method = value;
                    break;
            }

            return this;
        }

        public new IHerculesTagsBuilder AddValue(string key, int value)
        {
            if (key == WellKnownAnnotations.Http.Response.Code)
                Code = (ResponseCode)value;

            return this;
        }
    }
}