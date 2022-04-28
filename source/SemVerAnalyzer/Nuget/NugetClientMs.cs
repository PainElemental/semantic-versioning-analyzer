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

			string nugetFeed = _config.RepositoryUrl;
			SourceRepository repository = Repository.Factory.GetCoreV3(nugetFeed);

			SourceCacheContext cache = new SourceCacheContext();
			PackageSearchResource packageSearchResource = await repository.GetResourceAsync<PackageSearchResource>();
			FindPackageByIdResource findPackageByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>();
			SearchFilter searchFilter = new SearchFilter(includePrerelease: includePrerelease);

			int skip = 0;
			IPackageSearchMetadata matchingPackage = null;
			while (true)
			{
				var results = (await packageSearchResource.SearchAsync(
					packageName, // search string
					searchFilter,
					skip: skip,
					take: 20,
					logger,
					cancellationToken)).ToList();

				if (results.Count == 0)
				{
					break;
				}
				skip += results.Count;


				foreach (IPackageSearchMetadata result in results)
				{
					if (result.Identity.Id == packageName)
					{
						//Console.WriteLine($"Found matching package '{result.Identity.Id}' in feed '{nugetFeed}'");
						matchingPackage = result;
						break;
					}
				}
			}

			if (matchingPackage == null)
			{
				comments.Add($"Error - package '{packageName}' not found in feed '{nugetFeed}'");
				return null;
			}

			//get all versions
			var versions = await matchingPackage.GetVersionsAsync();


			VersionInfo matchingVersionInfo = null;
			if (string.IsNullOrEmpty(packageVersion))
			{
				Dictionary<Semver, VersionInfo> dicVersions = new Dictionary<Semver, VersionInfo>();
				foreach (var v in versions)
				{
					var semVer = v.Version.OriginalVersion.ToSemver();
					dicVersions.Add(semVer, v);
				}

				//find the highest version
				var highestSemVer = dicVersions.Keys.OrderByDescending(v => v).First();
				matchingVersionInfo = dicVersions[highestSemVer];
			}
			else
			{
				foreach (var v in versions)
				{
					if (v.Version.OriginalVersion == packageVersion)
					{
						matchingVersionInfo = v;
						break;
					}
				}
			}

			if (matchingVersionInfo == null)
			{
				comments.Add($"Error - package '{packageName}' not found in version '{packageVersion}' in feed '{nugetFeed}'");
				return null;
			}


			byte[] bytes;
			try
			{
				await using (var packageStream = new MemoryStream())
				{
					await findPackageByIdResource.CopyNupkgToStreamAsync(
						matchingPackage.Identity.Id, // package id
						matchingVersionInfo.Version,
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
