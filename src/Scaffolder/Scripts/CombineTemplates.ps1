param(
    [string]$TemplatesDir,
    [string]$OutputFile
)

$output = @()
Get-ChildItem "$TemplatesDir\*.scriban" | ForEach-Object {
    $header = "=" * 20 + " " + $_.BaseName + " " + "=" * 20
    $content = Get-Content $_.FullName -Raw
    $output += "`n$header`n$content"
}
$output | Set-Content -Path $OutputFile -Encoding UTF8