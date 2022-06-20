using System;
using System.Text.RegularExpressions;
using Pushpay.SemVerAnalyzer.Abstractions;

namespace Pushpay.SemVerAnalyzer.Engine
{
	public static class SemverExtensions
	{
		//check regex at "https://regex101.com/"
		//check this:
		//2.0.16
		//2.0.16-.NET Standard 2.0
		//2.0.16+84be3305c85c667b5e7643840c529f58e6ace723
		//2.0.16-.NET Standard 2.0+84be3305c85c667b5e7643840c529f58e6ace723

		//2.0.16.5
		//2.0.16.5-.NET Standard 2.0
		//2.0.16.5+84be3305c85c667b5e7643840c529f58e6ace723
		//2.0.16.5-.NET Standard 2.0+84be3305c85c667b5e7643840c529f58e6ace723

		//static readonly Regex _versionFormat = new Regex(@"^(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)((?<prerelease>-\w*)|(?<trailer>\.0))?(?<githash>\+[0-9a-f]{40})?$");
		//static readonly Regex _versionFormat = new Regex(@"^(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)((?<prerelease>-[\w\./\+]*)|(?<trailer>\.0))?(?<githash>\+[0-9a-f]{40})?$");
		//static readonly Regex _versionFormat = new Regex(@"^(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)((?<prerelease>-[\w\.' ']*)|(?<trailer>\.0))?(?<githash>\+[0-9a-f]{40})?$");

		static readonly Regex _versionFormat3Part = new Regex(@"^(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)(?<prerelease>-[\w\. ]*)?(?<githash>\+[0-9a-f]{40})?$");
		static readonly Regex _versionFormat4Part = new Regex(@"^(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)\.(?<trailer>[0-9]+)(?<prerelease>-[\w\. ]*)?(?<githash>\+[0-9a-f]{40})?$");

		public static int MajorVersion(this string version) => version.ToSemver().Major;
		public static int MinorVersion(this string version) => version.ToSemver().Minor;
		public static int PatchVersion(this string version) => version.ToSemver().Patch;

		public static string GetSemVer(string version) => version.ToSemver().ToString();

		public static string GetSuggestedVersion(this string version, VersionBumpType bump) => version.ToSemver().GetSuggestedVersionAfterBump(bump);

		public static Semver ToSemver(this string version)
		{
			//first try the regex for 3-part version number
			var match = _versionFormat3Part.Match(version);
			if (!match.Success)
			{
				//try the regex for 4-part version number
				match = _versionFormat4Part.Match(version);
			}

			//still no match? Throw exception!
			if (!match.Success)
			{
				throw new FormatException($"Not a version: '{version}'");
			}

			var major = int.Parse(match.Groups["major"].Value);
			var minor = int.Parse(match.Groups["minor"].Value);
			var patch = int.Parse(match.Groups["patch"].Value);
			var prerelease = match.Groups["prerelease"].Value;
			var trailer = match.Groups["trailer"].Value;
			return new Semver(major, minor, patch, prerelease, trailer);
		}
	}
}
