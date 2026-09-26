param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin\Release')
)

$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
$framework = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = Join-Path $OutputDirectory 'MouseJiggle.exe'
$sources = @(
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
    '/target:winexe',
    '/platform:x86',
    '/optimize+',
    '/debug-',
    '/warn:4',
    ('/out:' + $output),
    ('/win32icon:' + (Join-Path $PSScriptRoot 'MouseJiggle.ico')),
    ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')),
    ('/reference:' + (Join-Path $framework 'System.dll')),
    ('/reference:' + (Join-Path $framework 'System.Core.dll')),
    ('/reference:' + (Join-Path $framework 'System.Drawing.dll')),
    ('/reference:' + (Join-Path $framework 'System.Windows.Forms.dll'))
) + $sources

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE."
}

Get-Item -LiteralPath $output
