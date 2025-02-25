![build](https://github.com/pushpay-labs/semantic-versioning-analyzer/workflows/Build%20and%20Test/badge.svg)
![test](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/katemelkova/db35a837ca2f0e463fa0fcf6160a5659/raw/semver-analyzer-test-results-badge.json)

<table>
  <tr>
    <th>SemVerAnalyzer</th>
    <th>SemVerAnalyzer.Abstractions</th>
  </tr>
  <tr>
    <td>
      <a href="https://www.nuget.org/packages/SemVerAnalyzer/">
        <img alt="NuGet version" src="https://img.shields.io/nuget/v/SemVerAnalyzer.svg?svg=true"></img><br>
        <img alt="NuGet version" src="https://img.shields.io/nuget/dt/SemVerAnalyzer.svg?svg=true"></img>
      </a>
    </td>
    <td>
      <a href="https://www.nuget.org/packages/SemVerAnalyzer.Abstractions/">
        <img alt="NuGet version" src="https://img.shields.io/nuget/v/SemVerAnalyzer.Abstractions.svg?svg=true"></img><br>
        <img alt="NuGet version" src="https://img.shields.io/nuget/dt/SemVerAnalyzer.Abstractions.svg?svg=true"></img>
      </a>
    </td>
  </tr>
</table>

# Semantic Versioning Analyzer

This tool compares two versions of the same assembly (one local and one from a Nuget feed), lists the changes to the public API, and suggests an appropriate [Semantic Versioning](https://semver.org/) increment.

By default, it compares the local assembly to a published assembly of the same name on Nuget.org.

## Usage

This is published as a dotnet tool.  To install:

```sh
dotnet tool install SemVerAnalyzer
```

The command line options are as follows:

```
  -a, --assembly            Required. The built assembly to test.

  -o, --outputPath          The output file path for the report.

  -c, --config              Required. Path to the configuration file.

  -r, --additional-rules    A path to a single assembly or folder of assemblies which contain additional
                            rules.  Overrides `additionalRules` setting in JSON configuration file.

  -p, --package-name        If the package name is different than the DLL file name, specify it here.

  --omit-disclaimer         Omits the disclaimer paragraph that appears at the top of the output.

  -h, --include-header      Includes a header with the assembly and package at the top of the output.

  --help                    Display this help screen.

  --version                 Display version information.
```

The dotnet command for the tool is `analyze-semver`. For example,

```sh
dotnet analyze-semver -a path/to/MyAssembly.dll -o results.txt -c ./config.json
```

## Configuration

- `settings`
  - `ruleOverrides` - Provides overrides for individual rules (see below).
- `nuget`
  - `repositoryUrl` - The URL to the Nuget feed where the existing assembly is published.

## Built-in Rules

- Major (breaking changes)
  - `AbstractMethodIsNotOverrideableRule`
  - `AbstractPropertyIsNotOverrideableRule`
  - `AbstractEventIsNotOverrideableRule`
  - `EnumMemberRemovedRule`
  - `EnumMemberValueChangedRule`
  - `EventOnInterfaceAddedRule`
  - `EventRemovedRule`
  - `MethodOnInterfaceAddedRule`
  - `MethodRemovedRule`
  - `NonAbstractMethodHasBecomeAbstractRule`
  - `NonAbstractPropertyHasBecomeAbstractRule`
  - `NonAbstractEventHasBecomeAbstractRule`
  - `PropertyGetterRemovedRule`
  - `PropertyOnInterfaceAddedRule`
  - `PropertyOnInterfaceGetterAddedRule`
  - `PropertyOnInterfaceSetterAddedRule`
  - `PropertyRemovedRule`
  - `PropertySetterRemovedRule`
  - `ProtectedPropertyGetterNotAccessibleRule`
  - `ProtectedPropertySetterNotAccessibleRule`
  - `PublicPropertyGetterNotPublicRule`
  - `PublicPropertySetterNotPublicRule`
  - `ReferencesMajorBumpedRule`
  - `TypeRemovedRule`
  - `VirtualMethodHasBeenSealedRule`
  - `VirtualPropertyHasBeenSealedRule`
  - `VirtualEventHasBeenSealedRule`
  - `VirtualMethodIsNotVirtualRule`
  - `VirtualPropertyIsNotVirtualRule`
  - `VirtualEventIsNotVirtualRule`
- Minor (non-breaking additions)
  - `EnumMemberAddedRule`
  - `EventOnConcreteTypeAddedRule`
  - `InaccessiblePropertyGetterIsNowProtectedRule`
  - `InaccessiblePropertyGetterIsNowPublicRule`
  - `InaccessiblePropertySetterIsNowProtectedRule`
  - `InaccessiblePropertySetterIsNowPublicRule`
  - `MethodOnConcreteTypeAddedRule`
  - `PropertyOnConcreteTypeAddedRule`
  - `PropertyOnConcreteTypeGetterAddedRule`
  - `PropertyOnConcreteTypeSetterAddedRule`
  - `ReferencesMinorBumpedRule`
  - `TypeAddedRule`
  - `TypeMarkedObsoleteRule`
- Patch
  - `ReferencesPatchBumpedRule`
  - `VirtualMethodHasBeenOverriddenRule`
  - `VirtualPropertyHasBeenOverriddenRule`
  - `VirtualEventHasBeenOverriddenRule`
  - `VirtualMethodIsNoLongerOverriddenRule`
  - `VirtualPropertyIsNoLongerOverriddenRule`
  - `VirtualEventIsNoLongerOverriddenRule`

## Custom Rules

The `-r` / `--additional-rules` command switch takes a path to a single assembly or a folder that contains assemblies where additional rules are defined.  This can also be set in by the `settings.additionalRules` configuration setting.  *The command line switch will override the configuration setting.*

A rule is an implementation of `IVersionAnalysisRule<T>` where `T` is one of

- `AssemblyDef`
- `TypeDef`
- `PropertyDef`
- `MethodDef`
- `EventDef`

as defined by the [`dnlib`](https://github.com/0xd4d/dnlib) library.  The interface is defined in a separate Nuget package, `SemVerAnalyzer.Abstractions`, for this purpose.

To help with output, there are also a number of extension methods on the pertinent `dnlib` types that can generate C#-formatted names for members.

## Overriding Rule Severity

The built-in rules are defined with severities as listed above.  However, this can be overridden via the `settings.ruleOverrides` configuration setting.

The value for this setting is an object whose keys are rule names and values are one of

- `Ignore`
- `Major`
- `Minor`
- `Patch`

`Ignore` will prevent the rule from running, while this others will recategorize the rules as configured.
