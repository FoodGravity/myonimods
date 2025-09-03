# 调用通用打包脚本，包含额外的anim文件夹
$additionalFiles = @("D:\onimod\myonimods\装杯\anim")
& "D:\onimod\myonimods\--打包zip\通用打包脚本.ps1" -ModName "装杯" -AdditionalFiles $additionalFiles
