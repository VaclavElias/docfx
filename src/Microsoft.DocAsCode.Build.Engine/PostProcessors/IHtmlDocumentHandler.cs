﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HtmlAgilityPack;

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

public interface IHtmlDocumentHandler
{
    Manifest PreHandle(Manifest manifest);
    void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile);
    Manifest PostHandle(Manifest manifest);
}
