﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.UniversalReference;

[Serializable]
public class ApiLanguageValuePair<T>
{
    [YamlMember(Alias = "lang")]
    [JsonProperty("lang")]
    public string Language { get; set; }

    [YamlMember(Alias = "value")]
    [JsonProperty("value")]
    public T Value { get; set; }
}