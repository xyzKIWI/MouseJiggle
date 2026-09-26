param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin\Test')
)

$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
$framework = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = Join-Path $OutputDirectory 'SmokeTests.exe'
$sources = @(
    (Join-Path $PSScriptRoot 'tests\SmokeTests.cs'),
    (Join-Path $PSScriptRoot 'Program.cs'),
    (Join-Path $PSScriptRoot 'MainForm.cs'),
    (Join-Path $PSScriptRoot 'Jiggler.cs'),
    (Join-Path $PSScriptRoot 'PortableSettings.cs'),
    (Join-Path $PSScriptRoot 'StartupManager.cs'),
    (Join-Path $PSScriptRoot 'WindowThemeManager.cs'),
    (Join-Path $PSScriptRoot 'CenteredComboBox.cs'),
    (Join-Path $PSScriptRoot 'Properties\AssemblyInfo.cs')
)

$arguments = @(
    '/nologo',
    '/target:exe',
    '/platform:x86',
    '/optimize+',
    '/warn:4',
    '/main:ArkaneSystems.MouseJiggle.SmokeTests',
    ('/out:' + $output),
    ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')),
    ('/reference:' + (Join-Path $framework 'System.dll')),
    ('/reference:' + (Join-Path $framework 'System.Core.dll')),
    ('/reference:' + (Join-Path $framework 'System.Drawing.dll')),
    ('/reference:' + (Join-Path $framework 'System.Windows.Forms.dll'))
) + $sources

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE."
}

& $output
if ($LASTEXITCODE -ne 0) {
    throw "Smoke tests failed with exit code $LASTEXITCODE."
}
