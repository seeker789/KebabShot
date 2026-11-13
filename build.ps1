$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
try {
    $compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (!(Test-Path $compiler)) {
        $compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
    }
    & $compiler /nologo /target:winexe /optimize+ /win32icon:Kebabshot.ico /out:Kebabshot.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll Kebabshot.cs Editor.cs
    if ($LASTEXITCODE) { throw "Compilation failed." }
} finally {
    Pop-Location
}
