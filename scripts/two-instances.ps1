# Sends 40 parallel hold requests for a 5-seat class, half to port 8080 and half to port 8200.
Add-Type -AssemblyName System.Net.Http

$body = '{"name":"Two-process test","totalSeats":5}'
$cls = Invoke-RestMethod -Method Post -Uri http://localhost:8080/classes -ContentType "application/json" -Body $body

$http = New-Object System.Net.Http.HttpClient
$prefix = Get-Random -Minimum 1000000000000 -Maximum 9999999999999
$post = [System.Net.Http.HttpMethod]::Post

$tasks = foreach ($i in 1..40) {
    $port = if ($i % 2 -eq 0) { 8080 } else { 8200 }
    $url = "http://localhost:$port/classes/$($cls.id)/holds"
    $req = New-Object System.Net.Http.HttpRequestMessage -ArgumentList $post, $url
    $req.Headers.Add("X-Student-Id", ("{0}{1:D4}" -f $prefix, $i))
    $http.SendAsync($req)
}

[System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]$tasks)

$tasks | ForEach-Object { [int]$_.Result.StatusCode } | Group-Object | Select-Object Name, Count
Invoke-RestMethod "http://localhost:8080/classes/$($cls.id)"
