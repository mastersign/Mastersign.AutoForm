$binDir = Resolve-Path "$PSScriptRoot\..\Mastersign.AutoForm\bin\Release\netframework472"
$sourceBase = "`$(var.Mastersign.AutoForm.TargetDir)"
$include = @("*.dll")
$exclude = @("AutoForm.*")
$trgFile = "$PSScriptRoot\ProductDependencies.wxs"

$existingGuids = @{}
if (Test-Path $trgFile) {
    [xml]$wxs = Get-Content $trgFile
    $group = $wxs.Wix.Fragment.ComponentGroup
    $group.Component | ForEach-Object {
        [string]$filename = $_.File.Source
        if ($filename.StartsWith($sourceBase)) {
            $filename = $filename.Substring($sourceBase.Length)
        }
        $existingGuids[$filename] = $_.Guid
    }
}

$files = Get-ChildItem -Path $binDir -Recurse -Depth 0 -Include $include -Exclude $exclude `
    | Sort-Object -Property FullName

$begin = @"
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="ProductDependencyComponents">

"@
$end = @"

    </ComponentGroup>
  </Fragment>
</Wix>
"@

$components = @()

foreach ($file in $files) {
    $guid = if ($existingGuids.ContainsKey($file.Name)) {
        $existingGuids[$file.Name]
    } else {
        [Guid]::NewGuid().ToString("B")
    }
    $id = [IO.Path]::GetFileNameWithoutExtension($file.Name).Replace(".", "_")

    $components += @"
      <Component Id="Dependency_${id}" Guid="${guid}">
        <RegistryKey Root="HKCU" Key="Software\Mastersign\AutoForm\Dependencies">
          <RegistryValue KeyPath="yes"
                         Name="${id}_Component" Type="integer" Value="1" />
        </RegistryKey>
        <File Id="Dependency_${id}_FILE"
              Source="`$(var.Mastersign.AutoForm.TargetDir)$($file.Name)" />
      </Component>
"@
}

$xml = $begin + [string]::Join("`r`n", $components) + $end
$xml | Out-File $trgFile -Encoding utf8
