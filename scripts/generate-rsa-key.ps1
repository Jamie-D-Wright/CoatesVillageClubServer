$rsa = [System.Security.Cryptography.RSA]::Create(2048)
$xmlKey = $rsa.ToXmlString($true)
$base64Key = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($xmlKey))
Write-Output $base64Key
