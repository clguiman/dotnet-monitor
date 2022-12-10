﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class CounterPayload
    {
        [JsonPropertyName("provider")]
        public string Provider { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
