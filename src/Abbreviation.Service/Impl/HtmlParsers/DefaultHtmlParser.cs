//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System.Diagnostics;
using System.Linq;
using System.Text;
using Majestic13;

namespace Abbreviation.Service
{
    /// <summary>
    /// 基于Majestic13组件实现对Html的解析，解析出仅包含有用html文本信息的干净的html标签
    /// </summary>
    public class DefaultHtmlParser : HtmlParserBase, IHtmlParser
    {
        protected override string ProcessHtml(string html)
        {
            return GetAbbreviationHtml(html);
        }

        /// <summary>
        /// 处理指定的html内容，返回处理过的html内容；
        /// 处理过的内容不包含style, css, script, 以及其他一些不合法的html标签
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string GetAbbreviationHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            //分析html文档，将其解析为一颗树形结构
            var node = new HtmlParser().Parse(html);

            //获取html文档中的第一个body元素
            var visitor = new FindTagsVisitor(x => x.Name == "body");
            node.AcceptVisitor(visitor);

            //处理所有的Body元素
            var builder = new StringBuilder();
            foreach (var bodyNode in visitor.Result)
            {
                ProcessBodyElement(bodyNode as HtmlNode.Tag, builder);
            }

            //返回解析结果
            return builder.ToString();
        }
        /// <summary>
        /// 解析指定的一个Body元素
        /// </summary>
        /// <param name="bodyTag"></param>
        /// <param name="builder"></param>
        private void ProcessBodyElement(HtmlNode.Tag bodyTag, StringBuilder builder)
        {
            //修复input标签可能会包含子元素(如div,p)的Bug
            foreach (var childNode in bodyTag.Children.Clone())
            {
                var flag = false;
                do
                {
                    flag = false;
                    FixInputElementChildrenBug(childNode, bodyTag, ref flag);
                }
                while (flag);
            }

            //过滤出body中无效的标签
            foreach (var childNode in bodyTag.Children.Clone())
            {
                FilterInvalidNodes(childNode, bodyTag);
            }

            //过滤出body中的空标签
            foreach (var childNode in bodyTag.Children.Clone())
            {
                RemoveEmptyTags(childNode, bodyTag);
            }

            //将处理过的Body节点的所有子节点转换为html文本并返回
            foreach (var childNode in bodyTag.Children.Clone())
            {
                GenerateSimpleHtml(childNode, builder);
            }
        }
        /// <summary>
        /// 修正第三方组件分析html后的一个Bug，
        /// 第三方组件Majestic13.HtmlParser的分析结果有时会将元素作为input元素的子元素，而实际上并不是子元素。
        /// 所以，需要做一个修改，就是把input元素下的子元素调整为input父元素的子元素。
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="parentTag"></param>
        private void FixInputElementChildrenBug(HtmlNode currentNode, HtmlNode.Tag parentTag, ref bool flag)
        {
            if (currentNode is HtmlNode.Tag)
            {
                var currentTag = currentNode as HtmlNode.Tag;
                if (currentTag.Name == "input" && currentTag.Children.Count > 0)
                {
                    parentTag.Children.Remove(currentTag);
                    foreach (var childTag in currentTag.Children.Clone())
                    {
                        parentTag.Children.Add(childTag);
                    }
                    flag = true;
                }
                else
                {
                    foreach (var childNode in currentTag.Children.Clone())
                    {
                        if (childNode is HtmlNode.Tag)
                        {
                            FixInputElementChildrenBug(childNode, currentTag, ref flag);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 过滤出不需要的HtmlNode
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="parentNode"></param>
        private void FilterInvalidNodes(HtmlNode currentNode, HtmlNode.Tag parentTag)
        {
            if (currentNode is HtmlNode.Tag)
            {
                var currentTag = currentNode as HtmlNode.Tag;

                if (_filterTags.Contains(currentTag.Name))
                {
                    parentTag.Children.Remove(currentTag);
                }
                else
                {
                    foreach (var childNode in currentTag.Children.Clone())
                    {
                        if (childNode is HtmlNode.Tag)
                        {
                            FilterInvalidNodes(childNode, currentTag);
                        }
                    }
                }
            }
            else if (currentNode is HtmlNode.Script)
            {
                parentTag.Children.Remove(currentNode);
            }
            else if (currentNode is HtmlNode.Comment)
            {
                parentTag.Children.Remove(currentNode);
            }
        }
        /// <summary>
        /// 移除currentNode内的所有空HtmlNode
        /// </summary>
        /// <param name="currentNode">要扫描的HtmlNode</param>
        /// <param name="parentTag">要扫描的HtmlNode的父HtmlNode</param>
        private void RemoveEmptyTags(HtmlNode currentNode, HtmlNode.Tag parentTag)
        {
            if (currentNode is HtmlNode.Tag)
            {
                var currentTag = currentNode as HtmlNode.Tag;

                if (currentTag.IsEmptyTag())
                {
                    parentTag.Children.Remove(currentTag);
                }
                else
                {
                    foreach (var childNode in currentTag.Children.Clone())
                    {
                        RemoveEmptyTags(childNode, currentTag);
                    }
                    if (currentTag.IsEmptyTag())
                    {
                        parentTag.Children.Remove(currentTag);
                    }
                }
            }
        }
        /// <summary>
        /// 生成简单干净的html
        /// </summary>
        /// <param name="htmlNode"></param>
        /// <param name="builder"></param>
        private void GenerateSimpleHtml(HtmlNode htmlNode, StringBuilder builder)
        {
            if (htmlNode is HtmlNode.Text)
            {
                var value = (htmlNode as HtmlNode.Text).Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    builder.Append(value.Trim());
                }
            }
            else if (htmlNode is HtmlNode.Comment)
            {
                var value = (htmlNode as HtmlNode.Comment).Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    builder.Append(value.Trim());
                }
            }
            else if (htmlNode is HtmlNode.Tag)
            {
                var tag = htmlNode as HtmlNode.Tag;

                if (IsValidHrefTag(tag))
                {
                    builder.Append(BuildHrefTagString(tag));
                }
                else
                {
                    builder.Append(string.Format(_tagStart, tag.Name));
                }

                foreach (var childTag in tag.Children)
                {
                    GenerateSimpleHtml(childTag, builder);
                }

                builder.Append(string.Format(_tagEnd, tag.Name));
                if (tag.Name == "a" || tag.Name == "span")
                {
                    builder.Append("&nbsp;");
                }
            }
        }
        /// <summary>
        /// 返回指定的Tag是否是一个有效的(具有href属性)a标签
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private bool IsValidHrefTag(HtmlNode.Tag tag)
        {
            if (tag != null
                && tag.Name == "a"
                && tag.Attributes.Any(x => x.Key == "href" && !string.IsNullOrWhiteSpace(x.Value)))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 构建一个a标签字符串
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private string BuildHrefTagString(HtmlNode.Tag tag)
        {
            var href = tag.Attributes["href"];
            if (!href.StartsWith("http://") && !href.StartsWith("https://"))
            {
                href = _currentUrlDomain + href;
            }

            if (tag.Attributes.ContainsKey("target") && !string.IsNullOrEmpty(tag.Attributes["target"]))
            {
                return string.Format("<a href=\"{0}\" target=\"{1}\">", href, tag.Attributes["target"]);
            }
            else
            {
                return string.Format("<a href=\"{0}\">", href);
            }
        }

        #region 如果需要得到纯文本，则可以用这两个函数

        /// <summary>
        /// 获取Html的纯文本
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string GetRawText(string html)
        {
            var parser = new HtmlParser();
            var node = parser.Parse(html);
            var stringBuilder = new StringBuilder();
            GenerateRawText(node, stringBuilder);
            return stringBuilder.ToString();
        }
        /// <summary>
        /// 生成纯文本
        /// </summary>
        /// <param name="htmlNode"></param>
        /// <param name="builder"></param>
        private void GenerateRawText(HtmlNode htmlNode, StringBuilder builder)
        {
            if (htmlNode is HtmlNode.Text)
            {
                var value = (htmlNode as HtmlNode.Text).Value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }
                builder.Append(value.Trim());
                builder.Append(" "); //文本之间加一个空格
            }
            else if (htmlNode is HtmlNode.Tag)
            {
                foreach (var child in (htmlNode as HtmlNode.Tag).Children)
                {
                    GenerateRawText(child, builder);
                }
            }
        }

        #endregion
    }
}
