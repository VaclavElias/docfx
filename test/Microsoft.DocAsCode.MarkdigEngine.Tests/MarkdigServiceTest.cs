﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Xunit;

namespace Microsoft.DocAsCode.MarkdigEngine.Tests;

[Collection("docfx STA")]
public class MarkdigServiceTest
{
    [Fact]
    [Trait("Related", "MarkdigService")]
    public void MarkdigServiceTest_ParseAndRender_Simple()
    {
        var markdown = """
            # title

            ```yaml
            key: value
            ```
            """;
        var service = TestUtility.CreateMarkdownService();
        var document = service.Parse(markdown, "topic.md");

        Assert.Equal(2, document.Count);
        Assert.IsType<HeadingBlock>(document[0]);
        Assert.IsType<FencedCodeBlock>(document[1]);

        var mr = service.Render(document);
        var expected = """
            <h1 id="title">title</h1>
            <pre><code class="lang-yaml">key: value
            </code></pre>

            """;
        Assert.Equal(expected.Replace("""


            """, """


            """), mr.Html);
    }

    [Fact]
    [Trait("Related", "MarkdigService")]
    public void MarkdigServiceTest_ParseAndRender_Inclusion()
    {
        // -x
        //  |- root.md
        //  |- b
        //  |  |- linkAndRefRoot.md
        var root = @"[!include[linkAndRefRoot](~/x/b/linkAndRefRoot.md)]";
        var linkAndRefRoot = @"Paragraph1";
        TestUtility.WriteToFile("x/root.md", root);
        TestUtility.WriteToFile("x/b/linkAndRefRoot.md", linkAndRefRoot);

        var service = TestUtility.CreateMarkdownService();
        var document = service.Parse(root, "x/root.md");

        Assert.Single(document);
        Assert.IsType<InclusionBlock>(document[0]);

        var mr = service.Render(document);
        var expected = @"<p>Paragraph1</p>" + """


            """;
        Assert.Equal(expected, mr.Html);

        var expectedDependency = new List<string> { "b/linkAndRefRoot.md" };
        Assert.Equal(expectedDependency.ToImmutableList(), mr.Dependency);
    }

    [Fact]
    [Trait("Related", "MarkdigService")]
    public void MarkdigServiceTest_ParseInline()
    {
        var content = @"# I am a heading";
        var service = TestUtility.CreateMarkdownService();
        var document = service.Parse(content, "topic.md", true);
        var result = service.Render(document, true).Html;

        Assert.Single(document);
        Assert.IsType<ParagraphBlock>(document[0]);
        Assert.Equal(content, result);
    }
}
