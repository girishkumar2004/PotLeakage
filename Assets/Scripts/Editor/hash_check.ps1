function Get-Sha([string]$text) {
    $b = [System.Text.Encoding]::UTF8.GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $h = $sha.ComputeHash($b)
    return -join ($h | ForEach-Object { $_.ToString("x2") })
}

Write-Host "Task 3 No Voice: " (Get-Sha "English:Pot leakage has occurred in Pot 69.")
Write-Host "Task 3 Mark:     " (Get-Sha "English:Technical_Mark:Pot leakage has occurred in Pot 69.")
