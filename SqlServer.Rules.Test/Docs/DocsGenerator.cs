using LoxSmoke.DocXml;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlServer.Rules.Design;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace SqlServer.Rules.Tests.Docs
{
    [TestClass]
    [TestCategory("Docs")]
    public class DocsGenerator
    {
        private readonly TestContext _testContext;

        public DocsGenerator(TestContext testContext)
        {
            _testContext = testContext;
        }

        [TestMethod]
        public void GenerateDocs()
        {
            var assembly = typeof(ObjectCreatedWithInvalidOptionsRule).Assembly;
            var assemblyPath = assembly.Location;
            var assemblyFolder = Path.GetDirectoryName(assemblyPath);
            var docsFolder = Path.Combine(assemblyFolder, "Docs");

            var rules = assembly.GetTypes()
                .Where(t => t.IsClass 
                    && !t.IsAbstract 
                    && t.IsSubclassOf(typeof(BaseSqlCodeAnalysisRule))
                    && t.GetCustomAttributes(typeof(ExportCodeAnalysisRuleAttribute), false).Any()
                )
                .ToList();

            var categories = rules.Select(t =>
            {
                var ruleAttribute = t.GetCustomAttributes(typeof(ExportCodeAnalysisRuleAttribute), false).FirstOrDefault() as ExportCodeAnalysisRuleAttribute;
                return ruleAttribute.Category;
            }).Distinct().Order().ToList();

            CreateFolders(docsFolder, categories);

            var xmlPath = assemblyPath.Replace(".dll", ".xml");
            Assert.IsTrue(File.Exists(xmlPath));
            var reader = new DocXmlReader(xmlPath);

            rules.ForEach(t =>
            {
                var comments = reader.GetTypeComments(t);
                var ruleAttribute = t.GetCustomAttributes(typeof(ExportCodeAnalysisRuleAttribute), false).FirstOrDefault() as ExportCodeAnalysisRuleAttribute;

                var elements = GetRuleElements(t, ruleAttribute);

                GenerateRuleMarkdown(comments, elements, ruleAttribute, Path.Combine(docsFolder, ruleAttribute.Category), t.Assembly.GetName().Name, t.Namespace, t.Name);
            });

            GenerateTocMarkdown(rules, null, categories, reader, docsFolder);
        }

        private static void CreateFolders(string docsFolder, List<string> categories)
        {
            if (!Directory.Exists(docsFolder))
            {
                Directory.CreateDirectory(docsFolder);
            }

            foreach (var category in categories)
            {
                var categoryFolder = Path.Combine(docsFolder, category);
                if (!Directory.Exists(categoryFolder))
                {
                    Directory.CreateDirectory(categoryFolder);
                }
            }
        }

        private List<string> GetRuleElements(Type type, ExportCodeAnalysisRuleAttribute attribute)
        {
            var elements = new List<string>();

            if (attribute.RuleScope == SqlRuleScope.Element)
            {
                var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);


                if (constructor != null)
                {
                    var instance = (BaseSqlCodeAnalysisRule)constructor.Invoke(null);
                    if (instance != null)
                    {
                        foreach (var item in instance.SupportedElementTypes)
                        {
                            elements.Add(item.Name.ToSentence());
                        }
                    }
                }
            }
            else
            {
                elements.Add("Model");
            }

            return elements.Order().ToList();
        }

        private void GenerateRuleMarkdown(TypeComments comments, List<string> elements, ExportCodeAnalysisRuleAttribute attribute, string docsFolder, string assemblyName, string nameSpace, string className)
        {
            var fullXml = "<comments>" + comments.FullCommentText.Trim() + "</comments>";
            var fullComments = new XmlDocument();
            fullComments.LoadXml(fullXml);

            var isIgnorable = fullComments.SelectSingleNode("comments/IsIgnorable")?.InnerText ?? "false";
            var friendlyName = fullComments.SelectSingleNode("comments/FriendlyName")?.InnerText;

            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                friendlyName = className.Trim().ToSentence();
            }

            var exampleMd = fullComments.SelectSingleNode("comments/ExampleMd")?.InnerText;

            var stringBuilder = new StringBuilder();

            var spaces = "  ";
            stringBuilder.AppendLine($"# SQL Server Rule: {attribute.Id.ToId()}");
            stringBuilder.AppendLine(spaces);
            stringBuilder.AppendLine("|    |    |");
            stringBuilder.AppendLine("|----|----|");
            stringBuilder.AppendLine($"| Assembly | {assemblyName} |");
            stringBuilder.AppendLine($"| Namespace | {nameSpace} |");
            stringBuilder.AppendLine($"| Class | {className} |");
            stringBuilder.AppendLine(spaces);
            stringBuilder.AppendLine("## Rule Information");
            stringBuilder.AppendLine(spaces);
            stringBuilder.AppendLine("|    |    |");
            stringBuilder.AppendLine("|----|----|");
            stringBuilder.AppendLine($"| Id | {attribute.Id.ToId()} |");
            stringBuilder.AppendLine($"| Friendly Name | {friendlyName} |");
            stringBuilder.AppendLine($"| Category | {attribute.Category} |");
            stringBuilder.AppendLine($"| Ignorable | {isIgnorable} |");
            stringBuilder.AppendLine($"| Applicable Types | {elements.First()}  |");

            if (elements.Count > 1)
            {
                elements.RemoveAt(0);

                foreach (var element in elements)
                {
                    stringBuilder.AppendLine($"|   | {element} |");
                }
            }

            stringBuilder.AppendLine(spaces);
            stringBuilder.AppendLine("## Description");
            stringBuilder.AppendLine(spaces);
            stringBuilder.AppendLine($"{attribute.Description}");

            stringBuilder.AppendLine(spaces);
            stringBuilder.AppendLine($"## Summary");
            stringBuilder.AppendLine(spaces);
            stringBuilder.AppendLine($"{comments.Summary}");

            if (!string.IsNullOrWhiteSpace(exampleMd))
            {
                stringBuilder.AppendLine(spaces);
                stringBuilder.AppendLine("### Examples");
                stringBuilder.AppendLine(spaces);
                stringBuilder.Append($"{exampleMd}");
            }

            if (!string.IsNullOrWhiteSpace(comments.Remarks))
            {
                stringBuilder.AppendLine(spaces);
                stringBuilder.AppendLine("### Remarks");
                stringBuilder.AppendLine(spaces);
                stringBuilder.Append($"{comments.Remarks}");
            }

            stringBuilder.AppendLine(spaces);
            //stringBuilder.AppendLine("<sub><sup>Generated by a tool</sup></sub>");
            stringBuilder.AppendLine("[Generated by a tool]");

            var filePath = Path.Combine(docsFolder, $"{attribute.Id.ToId()}.md");
            File.WriteAllText(filePath, stringBuilder.ToString(), Encoding.ASCII);
        }

        private void GenerateTocMarkdown(List<Type> sqlServerRules, List<Type> tSqlSmellRules, List<string> categories, DocXmlReader reader, string docsFolder)
        {
            var spaces = "  ";

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(spaces);
            stringBuilder.AppendLine("# Table of Contents");
            stringBuilder.AppendLine(spaces);

            foreach (var category in categories) 
            {
                stringBuilder.AppendLine(spaces);
                stringBuilder.AppendLine($"## {category}");
                stringBuilder.AppendLine(spaces);

                stringBuilder.AppendLine("| Rule Id | Friendly Name | Ignorable | Description | Example? |");
                stringBuilder.AppendLine("|----|----|----|----|----|");
                var categoryRules = sqlServerRules
                        .Where(t => ((ExportCodeAnalysisRuleAttribute)t.GetCustomAttributes(typeof(ExportCodeAnalysisRuleAttribute), false).FirstOrDefault()).Category == category)
                        .OrderBy(t => ((ExportCodeAnalysisRuleAttribute)t.GetCustomAttributes(typeof(ExportCodeAnalysisRuleAttribute), false).FirstOrDefault()).Id)
                        .ToList();
                foreach (var rule in categoryRules)
                {
                    var comments = reader.GetTypeComments(rule);
                    var fullXml = "<comments>" + comments.FullCommentText.Trim() + "</comments>";
                    var fullComments = new XmlDocument();
                    fullComments.LoadXml(fullXml);

                    var isIgnorable = fullComments.SelectSingleNode("comments/IsIgnorable")?.InnerText ?? "false";
                    var friendlyName = fullComments.SelectSingleNode("comments/FriendlyName")?.InnerText;

                    if (string.IsNullOrWhiteSpace(friendlyName))
                    {
                        friendlyName = rule.Name.ToSentence();
                    }

                    friendlyName.Replace("|", "&#124;");

                    isIgnorable = isIgnorable != "false" ? "Yes" : " ";

                    var exampleMd = fullComments.SelectSingleNode("comments/ExampleMd")?.InnerText;

                    exampleMd = string.IsNullOrWhiteSpace(exampleMd) ? " " : "Yes";

                    var ruleAttribute = (ExportCodeAnalysisRuleAttribute)rule.GetCustomAttributes(typeof(ExportCodeAnalysisRuleAttribute), false).FirstOrDefault();
                    var ruleLink = $"[{ruleAttribute.Id.ToId()}]({category}/{ruleAttribute.Id.ToId()}.md)";
                    stringBuilder.AppendLine($"| {ruleLink} | {friendlyName} | {isIgnorable} | {ruleAttribute.Description?.Replace("|", "&#124;")} | {exampleMd} |");
                }
            }

            stringBuilder.AppendLine(spaces);
            //stringBuilder.AppendLine("<sub><sup>Generated by a tool</sup></sub>");
            stringBuilder.AppendLine("[Generated by a tool]");

            File.WriteAllText(Path.Combine(docsFolder, "table_of_contents.md"), stringBuilder.ToString(), Encoding.ASCII);
        }
    }

    public static class Extensions
    {
        public static string ToSentence(this string input)
        {
            //return new Regex("(?=[A-Z][a-z])").Replace(input, " ").TrimStart();
            var parts = Regex.Split(input, @"([A-Z]?[a-z]+)").Where(str => !string.IsNullOrEmpty(str));
            return string.Join(' ', parts);
        }

        public static string ToId(this string Input)
        {
            return new string(Input.Split('.').Last());
        }
    }
}