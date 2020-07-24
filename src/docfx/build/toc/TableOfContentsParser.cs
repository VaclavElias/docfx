// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsParser
    {
        private readonly Input _input;
        private readonly MarkdownEngine _markdownEngine;
        private readonly DocumentProvider _documentProvider;

        public TableOfContentsParser(Input input, MarkdownEngine markdownEngine, DocumentProvider documentProvider)
        {
            _input = input;
            _markdownEngine = markdownEngine;
            _documentProvider = documentProvider;
        }

        public TableOfContentsNode Parse(FilePath file, ErrorBuilder errors)
        {
            return file.Format switch
            {
                FileFormat.Yaml => Deserialize(_input.ReadYaml(errors, file), errors),
                FileFormat.Json => Deserialize(_input.ReadJson(errors, file), errors),
                FileFormat.Markdown => ParseMarkdown(_input.ReadString(file), file, errors),
                _ => throw new NotSupportedException($"'{file}' is an unknown TOC file"),
            };
        }

        private static TableOfContentsNode Deserialize(JToken token, ErrorBuilder errors)
        {
            if (token is JArray tocArray)
            {
                // toc model
                return new TableOfContentsNode { Items = JsonUtility.ToObject<List<SourceInfo<TableOfContentsNode>>>(errors, tocArray) };
            }
            else if (token is JObject tocObject)
            {
                // toc root model
                return JsonUtility.ToObject<TableOfContentsNode>(errors, tocObject);
            }

            return new TableOfContentsNode();
        }

        private TableOfContentsNode ParseMarkdown(string content, FilePath file, ErrorBuilder errors)
        {
            var headingBlocks = new List<HeadingBlock>();
            var ast = _markdownEngine.Parse(errors, content, _documentProvider.GetDocument(file), MarkdownPipelineType.TocMarkdown);

            foreach (var block in ast)
            {
                switch (block)
                {
                    case HeadingBlock headingBlock:
                        headingBlocks.Add(headingBlock);
                        break;
                    case YamlFrontMatterBlock _:
                    case HtmlBlock htmlBlock when htmlBlock.Type == HtmlBlockType.Comment:
                        break;
                    default:
                        errors.Add(Errors.TableOfContents.InvalidTocSyntax(block.GetSourceInfo()));
                        break;
                }
            }

            using var reader = new StringReader(content);
            return new TableOfContentsNode { Items = BuildTree(errors, headingBlocks) };
        }

        private List<SourceInfo<TableOfContentsNode>> BuildTree(ErrorBuilder errors, List<HeadingBlock> blocks)
        {
            if (blocks.Count <= 0)
            {
                return new List<SourceInfo<TableOfContentsNode>>();
            }

            var result = new TableOfContentsNode();
            var stack = new Stack<(int level, TableOfContentsNode item)>();

            // Level of root node is determined by its first child
            var parent = (level: blocks[0].Level - 1, node: result);
            stack.Push(parent);

            foreach (var block in blocks)
            {
                var currentLevel = block.Level;
                var currentItem = GetItem(errors, block);
                if (currentItem == null)
                {
                    continue;
                }

                while (stack.TryPeek(out parent) && parent.level >= currentLevel)
                {
                    stack.Pop();
                }

                if (parent.node is null || currentLevel != parent.level + 1)
                {
                    errors.Add(Errors.TableOfContents.InvalidTocLevel(block.GetSourceInfo(), parent.level, currentLevel));
                }
                else
                {
                    parent.node.Items.Add(currentItem.Value);
                }

                stack.Push((currentLevel, currentItem));
            }

            return result.Items;
        }

        private SourceInfo<TableOfContentsNode>? GetItem(ErrorBuilder errors, HeadingBlock block)
        {
            var source = block.GetSourceInfo();
            var currentItem = new TableOfContentsNode();
            if (block.Inline is null || !block.Inline.Any())
            {
                currentItem.Name = new SourceInfo<string?>(null, source);
                return new SourceInfo<TableOfContentsNode>(currentItem, source);
            }

            if (block.Inline.Count() > 1 && block.Inline.Any(l => l is XrefInline || l is LinkInline))
            {
                errors.Add(Errors.TableOfContents.InvalidTocSyntax(block.GetSourceInfo()));
                return null;
            }

            var xrefLink = block.Inline.FirstOrDefault(l => l is XrefInline);
            if (xrefLink != null && xrefLink is XrefInline xrefInline && !string.IsNullOrEmpty(xrefInline.Href))
            {
                currentItem.Uid = new SourceInfo<string?>(xrefInline.Href, xrefInline.GetSourceInfo());
                return new SourceInfo<TableOfContentsNode>(currentItem, source);
            }

            var link = block.Inline.FirstOrDefault(l => l is LinkInline);
            if (link != null && link is LinkInline linkInline)
            {
                if (!string.IsNullOrEmpty(linkInline.Url))
                {
                    currentItem.Href = new SourceInfo<string?>(linkInline.Url, linkInline.GetSourceInfo());
                }
                if (!string.IsNullOrEmpty(linkInline.Title))
                {
                    currentItem.DisplayName = linkInline.Title;
                }
                currentItem.Name = new SourceInfo<string?>(_markdownEngine.ToPlainText(linkInline), linkInline.GetSourceInfo());
            }

            if (currentItem.Name.Value is null)
            {
                currentItem.Name = new SourceInfo<string?>(_markdownEngine.ToPlainText(block.Inline), block.Inline.GetSourceInfo());
            }

            return new SourceInfo<TableOfContentsNode>(currentItem, source);
        }
    }
}
