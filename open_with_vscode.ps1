param(
    [Parameter(Mandatory=$false, ValueFromPipeline=$true)]
    [string[]]$FilePath
)

# 如果没有传入参数，则打开固定路径
if (!$FilePath) {
    $FilePath = "C:\Users\13091\AppData\LocalLow\Klei\Oxygen Not Included\Player.log"
}

foreach ($file in $FilePath) {
    code $file
}
