using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pushpay.SemVerAnalyzer.Nuget
{
	public interface INugetClient
	{
		Task<byte[]> GetAssemblyBytesFromPackage(string packageName, string packageVersion, bool includePrerelease, string fileName, string framework, List<string> comments);
	}
}
