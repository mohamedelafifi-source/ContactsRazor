# Password Generator Script for Golf Application (PowerShell)
# Generates secure passwords for initial user setup

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Golf Application - Password Generator" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script generates secure passwords for:"
Write-Host "- 1 Federation user"
Write-Host "- 10 Club Captain users"
Write-Host ""
Write-Host "Generated passwords:"
Write-Host ""

# Function to generate random password
function Generate-Password {
    param([int]$Length = 14)
    $chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*"
    $password = ""
    for ($i = 0; $i -lt $Length; $i++) {
        $password += $chars[(Get-Random -Maximum $chars.Length)]
    }
    return $password
}

# Generate Federation password
$FED_PASSWORD = Generate-Password -Length 14
Write-Host "Federation:" -ForegroundColor Yellow
Write-Host "  Username: federation"
Write-Host "  Password: $FED_PASSWORD"
Write-Host ""

# Generate passwords for 10 clubs
Write-Host "Club Captains:" -ForegroundColor Yellow
$passwords = @()
for ($i = 1; $i -le 10; $i++) {
    $password = Generate-Password -Length 14
    $passwords += [PSCustomObject]@{
        Username = "club${i}_captain"
        Password = $password
        ClubCode = "CLB$i"
    }
    Write-Host "  Club $i:"
    Write-Host "    Username: club${i}_captain"
    Write-Host "    Password: $password"
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "CSV Format (for import):" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Username,Password,ClubCode,Role"
Write-Host "federation,$FED_PASSWORD,FEDR,Federation"
foreach ($pwd in $passwords) {
    Write-Host "$($pwd.Username),$($pwd.Password),$($pwd.ClubCode),ClubCaptain"
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Red
Write-Host "⚠️  IMPORTANT: Save these passwords securely!" -ForegroundColor Red
Write-Host "==========================================" -ForegroundColor Red
