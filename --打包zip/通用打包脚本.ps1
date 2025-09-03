param(
    [Parameter(Mandatory=$true)]
    [string]$ModName,

    [Parameter(Mandatory=$false)]
    [string[]]$AdditionalFiles = @()
)

# 基础路径
$basePath = "D:\onimod\myonimods"

# 定义要打包的文件和文件夹路径（基础文件）
$filesToPack = @(
    "$basePath\$ModName\bin\Debug\$ModName.dll",
    "$basePath\$ModName\mod_info.yaml",
    "$basePath\$ModName\mod.yaml"
)

# 添加额外的文件/文件夹
$filesToPack += $AdditionalFiles

# 读取并更新 mod_info.yaml 版本号
$modInfoPath = "$basePath\$ModName\mod_info.yaml"
$modInfoContent = Get-Content $modInfoPath
$modInfoContent | Where-Object { $_ -match '^version:\s*(\d+\.\d+\.\d+)' } | Out-Null
$currentVersion = $matches[1]
$versionParts = $currentVersion.Split('.')
$versionParts[2] = [int]$versionParts[2] + 1
$newVersion = $versionParts -join '.'
$modInfoContent = $modInfoContent -replace "version: $currentVersion", "version: $newVersion"
$modInfoContent | Set-Content $modInfoPath

# 定义输出ZIP文件路径（带版本号）
$zipDir = "$basePath\--打包zip"
$outputZip = Join-Path $zipDir "${ModName}_v$newVersion.zip"

# 如果ZIP文件已存在则删除
if (Test-Path $outputZip) {
    Remove-Item $outputZip
}

# 创建临时目录
$tempDir = "$basePath\${ModName}_temp"
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
