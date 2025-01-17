﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Microsoft.DocAsCode.Plugins;

public class TreeItemRestructure
{
    public string Key { get; set; }

    public TreeItemKeyType TypeOfKey { get; set; }

    public TreeItemActionType ActionType { get; set; }

    public IImmutableList<TreeItem> RestructuredItems { get; set; }

    /// <summary>
    /// Specifies which files trigger the restructure
    /// </summary>
    public IImmutableList<FileAndType> SourceFiles { get; set; }
}
