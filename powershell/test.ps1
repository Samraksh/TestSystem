$test_pattern = @(15,31,63,127,255,239,175,143,15,143,15,31,95,31,63,47,15)
Write-Sam "test started..."
Start-Logic
sleep 5
Continue-GDB
foreach($i in $test_pattern) {
	do {
	$byte = Read-Byte-Logic
	Write-Sam "$byte - $i"
	} while($byte -eq $i)
}
Write-Host "done"
sleep 5