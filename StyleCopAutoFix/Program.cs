using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using StyleCop;

namespace StyleCopAutoFix
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("USAGE: StyleCopAutoFix sln_filepath|csproj_filepath|cs_filepath");
				return;
			}

			//fix rules one by one
			//same line number can be reported several times by StyleCop (but for different rules), it is important to fix rules one by one.
			string[] rules = new string[] { "SA1514", "SA1512", "SA1516", "SA1507", "SA1508", "SA1518", "SA1505", "SA1513", "SA1515", "SA1517" };
			int totalViolationsFound = 0, totalViolationsFixed = 0;

			bool countViolations = true;
			foreach (string rule in rules)
			{
				FixStyleCopRule(args[0], rule, (sender, e) =>
				{
					if(countViolations)
					{
						totalViolationsFound++;
					}

					if (e.Violation.Rule.CheckId == rule)
					{
						totalViolationsFixed++;
					}
				});
				countViolations = false;
			}

			Console.WriteLine("StyleCop violations found : {0}", totalViolationsFound);
			Console.WriteLine("StyleCop violations fixed : {0}", totalViolationsFixed);
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}

		private static void FixStyleCopRule(string projectFilePath, string rule, EventHandler<ViolationEventArgs> onViolationEncountered)
		{
			foreach (string filePath in GetCSharpFiles(projectFilePath))
			{
				StyleCopConsole console = new StyleCopConsole(null, false, null, null, true);

				CodeProject project = new CodeProject(0, Path.GetDirectoryName(projectFilePath), new Configuration(null));

				bool fileHasBeenFixed = false;

				List<Tuple<int, string>> sourceCode = File.ReadAllText(filePath).Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
					.Select((line, index) => new Tuple<int, string>(index + 1, line)).ToList();

				if (console.Core.Environment.AddSourceCode(project, filePath, null))
				{
					console.ViolationEncountered += onViolationEncountered;
					console.ViolationEncountered += (sender, e) =>
					{
						if (e.Violation.Rule.CheckId == rule)
						{
							FixStyleCopViolation(sourceCode, e);
							Console.WriteLine("{0}({1}): {2}", rule, e.LineNumber, filePath);
							fileHasBeenFixed = true;
						}
					};
					console.Start(new[] { project }, true);
				}

				if (fileHasBeenFixed)
				{
					//preserve text encoding
					System.Text.Encoding encoding;
					using (StreamReader reader = new StreamReader(filePath, true))
					{
						encoding = reader.CurrentEncoding;
					}

					File.WriteAllText(filePath, string.Join(Environment.NewLine, sourceCode.Select(x => x.Item2)), encoding);
				}
			}
		}

		private static IEnumerable<string> GetCSharpFiles(string filePath)
		{
			switch (Path.GetExtension(filePath))
			{
				case ".sln":
					return GetCSharpFilesInSolution(filePath);

				case ".csproj":
					return GetCSharpFilesInProject(filePath);

				case ".cs":
					return new string[] { filePath };

				default:
					throw new Exception("Invalid command line argument: " + filePath);
			}
		}

		private static IEnumerable<string> GetCSharpFilesInSolution(string filePath)
		{
			string directoryName = Path.GetDirectoryName(filePath);
			Regex r = new Regex("^Project\\(\"(.*)\"\\) = \"(.*)\", \"(.*)\", \"(.*)\"$", RegexOptions.Multiline);
			return File.ReadAllLines(filePath).SelectMany(x => r.Matches(x).Cast<Match>().SelectMany(y => GetCSharpFilesInProject(Path.Combine(directoryName, y.Groups[3].Value))));
		}

		private static IEnumerable<string> GetCSharpFilesInProject(string filePath)
		{
			string directoryName = Path.GetDirectoryName(filePath);
			XDocument project = XDocument.Load(filePath);
			return
			 from el in project.Root
				 .Elements(XName.Get("ItemGroup", @"http://schemas.microsoft.com/developer/msbuild/2003"))
				 .Elements(XName.Get("Compile", @"http://schemas.microsoft.com/developer/msbuild/2003"))
			 select Path.Combine(directoryName, el.Attribute("Include").Value);
		}

		private static void AddEmptyLine(List<Tuple<int, string>> sourceCode, int lineNumber)
		{
			sourceCode.Insert(sourceCode.FindIndex(x => x.Item1 == lineNumber), new Tuple<int, string>(-1, string.Empty));
		}
		
		private static void RemoveEmptyLine(List<Tuple<int, string>> sourceCode, int lineNumber)
		{
			Debug.Assert(string.IsNullOrEmpty(sourceCode[sourceCode.FindIndex(x => x.Item1 == lineNumber)].Item2.Trim()));
			sourceCode.RemoveAt(sourceCode.FindIndex(x => x.Item1 == lineNumber));
		}

		private static void FixStyleCopViolation(List<Tuple<int, string>> sourceCode, ViolationEventArgs e)
		{
			switch (e.Violation.Rule.CheckId)
			{
				//ElementsMustBeSeparatedByBlankLine
				case "SA1516":
					AddEmptyLine(sourceCode, e.LineNumber);
					break;

				//CodeMustNotContainMultipleBlankLinesInARow
				case "SA1507":
					RemoveEmptyLine(sourceCode, e.LineNumber);
					break;

				//ClosingCurlyBracketsMustNotBePrecededByBlankLine
				case "SA1508":
					RemoveEmptyLine(sourceCode, e.LineNumber - 1);
					break;

				//CodeMustNotContainBlankLinesAtEndOfFile
				case "SA1518":
					RemoveEmptyLine(sourceCode, e.LineNumber);
					break;

				//OpeningCurlyBracketsMustNotBeFollowedByBlankLine
				case "SA1505":
					RemoveEmptyLine(sourceCode, e.LineNumber+1);
					break;

				//ClosingCurlyBracketMustBeFollowedByBlankLine
				case "SA1513":
					AddEmptyLine(sourceCode, e.LineNumber + 1);
					break;

				//SingleLineCommentsMustBePrecededByBlankLine
				case "SA1515":
					AddEmptyLine(sourceCode, e.LineNumber);
					break;

				//ElementDocumentationHeadersMustBePrecededByBlankLine
				case "SA1514":
					AddEmptyLine(sourceCode, e.LineNumber);
					break;

				//SingleLineCommentsMustNotBeFollowedByBlankLine
				case "SA1512":
					RemoveEmptyLine(sourceCode, e.LineNumber + 1);
					break;

				//CodeMustNotContainBlankLinesAtStartOfFile
				case "SA1517":
					RemoveEmptyLine(sourceCode, e.LineNumber);
					break;

				default:
					throw new NotImplementedException();
			}
		}
	}
}