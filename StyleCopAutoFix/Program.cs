using StyleCop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StyleCopAutoFix
{
	class Program
	{
		static bool fileHasBeenFixed;
		static List<Tuple<int, string>> sourceCode;
		static string currentRule;
		static int totalViolationsFound, totalViolationsFixed;

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("USAGE: StyleCopAutoFix sln_filepath or StyleCopAutoFix csproj_filepath");
				return;
			}

			//fix rules one by one 
			//same line number can be reported several times by StyleCop (but for different rules), it is important to fix rules one by one.
			string[] rules = new string[] { "SA1516", "SA1507", "SA1508", "SA1518", "SA1505", "SA1513", "SA1515" };

			foreach (string rule in rules)
			{
				FixStyleCopRule(args[0], rule);
			}
			Console.WriteLine("StyleCop violations found : {0}", totalViolationsFound);
			Console.WriteLine("StyleCop violations fixed : {0}", totalViolationsFixed);
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}

		private static void FixStyleCopRule(string solutionFullPath, string rule)
		{
			foreach (string filePath in GetCSharpFiles(solutionFullPath))
			{
				StyleCopConsole console = new StyleCopConsole(null, false, null, null, true);

				CodeProject project = new CodeProject(0, Path.GetDirectoryName(solutionFullPath), new Configuration(null));

				currentRule = rule;
				fileHasBeenFixed = false;
				sourceCode = File.ReadAllText(filePath).Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
					.Select((line, index) => new Tuple<int, string>(index + 1, line)).ToList();

				if (console.Core.Environment.AddSourceCode(project, filePath, null))
				{
					console.ViolationEncountered += OnViolationEncountered;
					console.Start(new[] { project }, true);
					console.ViolationEncountered -= OnViolationEncountered;
				}

				if (fileHasBeenFixed)
				{
					Console.WriteLine("{0}: {1}", currentRule, filePath);
					File.WriteAllText(filePath, string.Join(Environment.NewLine, sourceCode.Select(x => x.Item2)));
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

		static void OnViolationEncountered(object sender, ViolationEventArgs e)
		{
			if (e.Violation.Rule.CheckId == currentRule)
			{
				switch (e.Violation.Rule.CheckId)
				{
					//ElementsMustBeSeparatedByBlankLine
					case "SA1516":
						sourceCode.Insert(sourceCode.FindIndex(x => x.Item1 == e.LineNumber), new Tuple<int, string>(-1, string.Empty));
						fileHasBeenFixed = true;
						totalViolationsFixed++;
						break;

					//CodeMustNotContainMultipleBlankLinesInARow				
					case "SA1507":
						Debug.Assert(string.IsNullOrEmpty(sourceCode[sourceCode.FindIndex(x => x.Item1 == e.LineNumber)].Item2.Trim()));
						sourceCode.RemoveAt(sourceCode.FindIndex(x => x.Item1 == e.LineNumber));
						fileHasBeenFixed = true;
						totalViolationsFixed++;
						break;

					//ClosingCurlyBracketsMustNotBePrecededByBlankLine
					case "SA1508":
						Debug.Assert(string.IsNullOrEmpty(sourceCode[sourceCode.FindIndex(x => x.Item1 == e.LineNumber - 1)].Item2.Trim()));
						sourceCode.RemoveAt(sourceCode.FindIndex(x => x.Item1 == e.LineNumber - 1));
						fileHasBeenFixed = true;
						totalViolationsFixed++;
						break;

					//CodeMustNotContainBlankLinesAtEndOfFile
					case "SA1518":
						Debug.Assert(string.IsNullOrEmpty(sourceCode[sourceCode.FindIndex(x => x.Item1 == e.LineNumber)].Item2.Trim()));
						sourceCode.RemoveAt(sourceCode.FindIndex(x => x.Item1 == e.LineNumber));
						fileHasBeenFixed = true;
						totalViolationsFixed++;
						break;

					//OpeningCurlyBracketsMustNotBeFollowedByBlankLine
					case "SA1505":
						Debug.Assert(string.IsNullOrEmpty(sourceCode[sourceCode.FindIndex(x => x.Item1 == e.LineNumber + 1)].Item2.Trim()));
						sourceCode.RemoveAt(sourceCode.FindIndex(x => x.Item1 == e.LineNumber + 1));
						fileHasBeenFixed = true;
						totalViolationsFixed++;
						break;

					//ClosingCurlyBracketMustBeFollowedByBlankLine
					case "SA1513":
						sourceCode.Insert(sourceCode.FindIndex(x => x.Item1 == e.LineNumber + 1), new Tuple<int, string>(-1, string.Empty));
						fileHasBeenFixed = true;
						totalViolationsFixed++;
						break;

					//SingleLineCommentsMustBePrecededByBlankLine
					case "SA1515":
						sourceCode.Insert(sourceCode.FindIndex(x => x.Item1 == e.LineNumber), new Tuple<int, string>(-1, string.Empty));
						fileHasBeenFixed = true;
						totalViolationsFixed++;
						break;
				}
			}
			totalViolationsFound++;
			//Console.WriteLine("{2} {0}: {1}", e.Violation.Rule.CheckId, e.Message, e.LineNumber);
		}
	}
}