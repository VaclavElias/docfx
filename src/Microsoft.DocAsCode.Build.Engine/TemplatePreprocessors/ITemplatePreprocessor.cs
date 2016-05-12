// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;

    public interface ITemplatePreprocessor
    {
        Func<object, object> GetOptionsFunc { get; }
        Func<object, object> TransformModelFunc { get; }
    }
}
