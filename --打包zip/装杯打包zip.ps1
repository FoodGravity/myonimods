# 定义要打包的文件和文件夹路径
$filesToPack = @(
    "D:\onimod\myonimods\装杯\bin\Debug\装杯.dll",
    "D:\onimod\myonimods\装杯\mod_info.yaml",
    "D:\onimod\myonimods\装杯\mod.yaml",
    "D:\onimod\myonimods\装杯\anim"
)

# 读取并更新 mod_info.yaml 版本号
$modInfoPath = "D:\onimod\myonimods\装杯\mod_info.yaml"
$modInfoContent = Get-Content $modInfoPath
$modInfoContent | Where-Object { $_ -match '^version:\s*(\d+\.\d+\.\d+)' } | Out-Null
$currentVersion = $matches[1]
$versionParts = $currentVersion.Split('.')
$versionParts[2] = [int]$versionParts[2] + 1
$newVersion = $versionParts -join '.'
$modInfoContent = $modInfoContent -replace "version: $currentVersion", "version: $newVersion"
$modInfoContent | Set-Content $modInfoPath

# 定义输出ZIP文件路径（带版本号）
$outputZip = "D:\onimod\myonimods\--打包zip\装杯_v$newVersion.zip"

# 如果ZIP文件已存在则删除
if (Test-Path $outputZip) {
    Remove-Item $outputZip
}

# 创建临时目录
$tempDir = "D:\onimod\myonimods\装杯_temp"
if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# 复制文件到临时目录
foreach ($file in $filesToPack) {
    if (Test-Path $file) {
        if ((Get-Item $file) -is [System.IO.DirectoryInfo]) {
            Copy-Item -Path $file -Destination $tempDir -Recurse
        } else {
            Copy-Item -Path $file -Destination $tempDir
        }
    } else {
        Write-Warning "文件或目录不存在: $file"
    }
}

# 创建ZIP文件
Add-Type -Assembly System.IO.Compression.FileSystem
$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
[System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $outputZip, $compressionLevel, $false)

# 清理临时目录
Remove-Item $tempDir -Recurse -Force

Write-Host "打包完成: $outputZip"
