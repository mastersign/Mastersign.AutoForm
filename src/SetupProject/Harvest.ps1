$binDir = Resolve-Path "$PSScriptRoot\..\Mastersign.AutoForm\bin\Release\netframework472"

$include = @("*.dll")
$exclude = @("AutoForm.*")

$files = Get-ChildItem -Path $binDir -Recurse -Depth 0 -Include $include -Exclude $exclude

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
    $guid = [Guid]::NewGuid().ToString("B")
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
$xml | Out-File "$PSScriptRoot\ProductDependencies.wxs" -Encoding utf8
