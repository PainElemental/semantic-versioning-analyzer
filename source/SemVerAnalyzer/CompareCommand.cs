using System.IO;
using System.Text.RegularExpressions;

using CommandLine;

namespace Pushpay.SemVerAnalyzer
{
	public class CompareCommand
	{
		[Option('a', "assembly", Required = true, HelpText = "The built assembly to test.")]
		public string Assembly { get; set; }

		[Option('s', "secondAssembly", Required = false, HelpText = "The second/old/current assembly to compare against. Use this as alternative to specifying a nuget feed in the config.")]
		public string OldAssembly { get; set; }

		[Option('o', "outputPath", Required = false, HelpText = "The output file path for the report.")]
		public string OutputPath { get; set; }

		[Option('c', "config", Required = false, HelpText = "Path to the configuration file.")]
		public string Configuration { get; set; }

		[Option('r', "additional-rules", Required = false, HelpText = "A path to a single assembly or folder of assemblies which contain additional rules.")]
		public string AdditionalRulesPath { get; set; }

		[Option('p', "package-name", HelpText = "If the package name is different than the DLL file name, specify it here.")]
		public string PackageName { get; set; }

		[Option('v', "package-version", HelpText = "If a specific package version should be downloaded from the feed, specify it here.")]
		public string PackageVersion { get; set; }

		[Option('i', "include-prerelease", HelpText = "Include prerelease packages when looking for the version in the feed")]
		public bool? IncludePrerelease { get; set; }

		[Option("omit-disclaimer", HelpText = "Omits the disclaimer paragraph that appears at the top of the output.")]
		public bool? OmitDisclaimer { get; set; }

		[Option('h', "include-header", HelpText = "Includes a header with the assembly and package at the top of the output.")]
		public bool? IncludeHeader { get; set; }

		[Option("assume-changes", HelpText = "Assumes that something changed, making Patch the lowest bump rather than None. Default is false.")]
		public bool? AssumeChanges { get; set; }

		[Option('f', "framework", Required = false, HelpText = "Indicates the framework from the Nuget package to use as a comparison.")]
		public string Framework { get; set; }

		public string FullAssemblyPath => Path.GetFullPath(Assembly);
		public string AssemblyFileName => Path.GetFileNameWithoutExtension(Assembly);

		public string OldAssemblyFullPath => OldAssembly != null ? Path.GetFullPath(OldAssembly) : string.Empty;
		public string OldAssemblyFileName => OldAssembly != null ? Path.GetFileNameWithoutExtension(OldAssembly) : string.Empty;

		public string Validate()
		{
			if (string.IsNullOrEmpty(Configuration))
			{
				//if no config file was specified, use the one in the current directory
				var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
				string currentDir = Path.GetDirectoryName(executingAssembly.Location);
				Configuration = Path.GetFullPath(Path.Combine(currentDir, "config.json"));
			}

			if (string.IsNullOrWhiteSpace(PackageName))
			{
				var match = Regex.Match(Assembly, @"^(.*(\/|\\))?(?<packageName>.*)\.dll$", RegexOptions.IgnoreCase);
				if (!match.Success)
					return "Cannot extract package name from provided assembly file name";
				PackageName = match.Groups["packageName"].Value;
			}

			if (!File.Exists(Assembly))
				return $"Cannot find assembly file '{Assembly}'";

			if (!File.Exists(Configuration))
				return $"Cannot find config file '{Configuration}'";

			return null;
		}
	}
}
