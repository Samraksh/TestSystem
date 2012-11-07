# helper functions
function SamTest-Help {
	Write-Host "Get-Credential"
	Write-Host "Enter-PsSession $ip_address"
	Write-Host "Git-Checkout $commit_id"
	Write-Host "cd C:\SamTest"
	Write-Host ". .\init.ps1"
	Write-Host ". .\test.ps1"
}

function Git-Checkout {
	param($commit)
    Set-Variable commit_id $commit -Scope Global
    Set-Varialbe checkout $true -Scope Global
}