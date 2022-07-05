// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Pushpay.SemVerAnalyzer.Engine;

namespace Pushpay.SemVerAnalyzer.Nuget
{
	/// <summary>
	/// A nuget client using the MS library 'NuGet.Protocol'
	/// https://github.com/NuGet/Home
	///
	/// See also https://markheath.net/post/download-and-push-nuget-csharp
	/// </summary>
	internal class NugetClientMs : INugetClient
	{
		readonly NugetConfiguration _config;
		readonly AppSettings _settings;

		public NugetClientMs(NugetConfiguration config, AppSettings settings)
		{
			_config = config;
			_settings = settings;
		}

		public async Task<byte[]> GetAssemblyBytesFromPackage(string packageName, string packageVersion, bool includePrerelease, string fileName, string framework, List<string> comments)
		{
			ILogger logger = NullLogger.Instance;
			CancellationToken cancellationToken = CancellationToken.None;

			SourceCacheContext cache = new SourceCacheContext();
			SourceRepository repository = Repository.Factory.GetCoreV3(_config.RepositoryUrl);
			FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

			var versions = await resource.GetAllVersionsAsync(packageName, cache, logger, cancellationToken);
			var versionsList = versions.ToList();

			//Console.WriteLine($"Found '{versionsList.Count}' versions of package '{packageName}'");


			//look for the correct version
			NuGet.Versioning.NuGetVersion matchingVersionInfo = null;
			if (!string.IsNullOrEmpty(packageVersion))
			{
				//look for the specified version
				foreach (var version in versionsList)
				{
					if (version.IsPrerelease &&
						includePrerelease == false)
					{
						continue;
					}

					if (version.OriginalVersion == packageVersion)
					{
						matchingVersionInfo = version;
						break;
					}
				}
			}
			else
			{
				Dictionary<Semver, NuGet.Versioning.NuGetVersion> dicVersions = new Dictionary<Semver, NuGet.Versioning.NuGetVersion>();
				foreach (var v in versions)
				{
					var semVer = v.OriginalVersion.ToSemver();
					dicVersions.Add(semVer, v);
				}

				var orderedVersions = dicVersions.Keys.OrderBy(v => v);

				if (includePrerelease)
				{
					//take the latest version
					var highestSemVer = orderedVersions.Last();
					matchingVersionInfo = dicVersions[highestSemVer];
				}
				else
				{
					//take the latest version that is not prerelease
					foreach (var version in orderedVersions)
					{
						var v = dicVersions[version];

						if (!v.IsPrerelease)
						{
							matchingVersionInfo = v;
						}
					}
				}
			}

			if (matchingVersionInfo == null)
			{
				comments.Add($"Error - package '{packageName}' not found in version '{packageVersion}' in feed '{_config.RepositoryUrl}'");
				return null;
			}
			else
			{
				//Console.WriteLine($"Downloading package '{packageName}' in version '{matchingVersionInfo.Version.OriginalVersion}' from feed '{nugetFeed}' ...");
			}

			byte[] bytes;
			try
			{
				await using (var packageStream = new MemoryStream())
				{
					await resource.CopyNupkgToStreamAsync(
						packageName, // package id
						matchingVersionInfo,
						packageStream,
						cache,
						logger,
						cancellationToken);
					//Console.WriteLine($"Downloaded version '{highestVersionInfo.Version}'");

					string frameworkNickname = GetFrameworkNickName(framework);

					using (var archive = new ZipArchive(packageStream))
					{
						ZipArchiveEntry entry = string.IsNullOrEmpty(_settings.Framework)
							? framework == null
								? archive.Entries.FirstOrDefault(e => e.FullName.EndsWith($"{fileName}.dll"))
								: archive.Entries.FirstOrDefault(e => e.FullName.Contains(frameworkNickname) && e.FullName.EndsWith($"{fileName}.dll")) ??
								  archive.Entries.FirstOrDefault(e => e.FullName.EndsWith($"{fileName}.dll"))
							: archive.Entries.FirstOrDefault(e => e.FullName.Contains(_settings.Framework) && e.FullName.EndsWith($"{fileName}.dll"));
						await using var unzippedEntryStream = entry?.Open();
						if (unzippedEntryStream == null)
						{
							comments.Add("Found NuGet package, but could not find DLL within it.");
							return null;
						}

						bytes = ReadAllBytes(unzippedEntryStream);
					}
				}
			}
			catch (Exception e)
			{
				comments.Add($"Error while downloading or reading package. Exception: {e}");
				return null;
			}

			return bytes;
		}


		// source strings following format from https://docs.microsoft.com/en-us/dotnet/api/system.runtime.versioning.targetframeworkattribute?view=net-5.0
		// target strings from https://docs.microsoft.com/en-us/dotnet/standard/frameworks
		// only including most common
		static string GetFrameworkNickName(string framework)
		{
			var parts = framework.Split(",Version=v");
			var major = int.Parse(parts[1].Split(".")[0]);
			return parts[0] switch
			{
				".NETFramework" => "net" + parts[1].Replace(".", ""),
				".NETStandard" => "netstandard" + parts[1],
				".NETCoreApp" => major < 4
					? "netcoreapp" + parts[1]
					: "net" + parts[1],
				_ => null
			};
		}

		static byte[] ReadAllBytes(Stream input)
		{
			var buffer = new byte[1 << 20];
			using var ms = new MemoryStream();
			int read;
			while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				ms.Write(buffer, 0, read);
			}

			return ms.ToArray();
		}
	}
}
