param([string]$RequestFile, [string]$OutputFile)
$ErrorActionPreference = 'Stop'
$client = [System.Net.Sockets.TcpClient]::new('127.0.0.1', 47901)
$client.ReceiveTimeout = 60000
$stream = $client.GetStream()
$writer = [System.IO.StreamWriter]::new($stream, [System.Text.UTF8Encoding]::new($false))
$writer.AutoFlush = $true
$reader = [System.IO.StreamReader]::new($stream)
try {
  $results = @()
  foreach ($line in Get-Content -LiteralPath $RequestFile) {
    if (-not $line.Trim()) { continue }
    $writer.WriteLine($line)
    $reply = $reader.ReadLine()
    if ($null -eq $reply) { throw 'Canal encerrado sem resposta.' }
    $result = $reply | ConvertFrom-Json
    $results += $result
    if (-not $result.ok) { throw $reply }
  }
  ConvertTo-Json -InputObject $results -Depth 100 | Set-Content -LiteralPath $OutputFile -Encoding utf8
  Write-Output "Respostas: $($results.Count). Arquivo: $OutputFile"
} finally { $client.Dispose() }
