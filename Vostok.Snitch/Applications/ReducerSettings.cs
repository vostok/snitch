﻿using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Applications
{
    [PublicAPI]
    public class ReducerSettings
    {
        [Required]
        public string SourceStream { get; set; }
    }
}