param (
    [int]$count = 200,
    [switch]$randomAssign = $false,
    [switch]$includeResolved = $true
)

$apiUrl = "https://localhost:5001/api/tickets"
$tokenUrl = "https://localhost:5001/api/account/login"

Write-Host "IT Helpdesk Ticketing System - Load Test Script" -ForegroundColor Cyan
Write-Host "-------------------------------------------" -ForegroundColor Cyan
Write-Host "This script will generate $count simulated support tickets"
Write-Host "Random assignment: $randomAssign"
Write-Host "Include resolved tickets: $includeResolved"
Write-Host ""

# Login credentials
$credentials = @{
    username = "admin@helpdesk.com"
    password = "Password123!"
}

# Request categories
$categories = @("Hardware", "Software", "Network", "Account", "Email", "Other")

# Priority levels
$priorities = @("Low", "Medium", "High", "Critical")

# Sample ticket titles
$titles = @(
    "Can't access my email",
    "Computer won't boot up",
    "Need software installed",
    "Printer not working",
    "Need password reset",
    "VPN connection issues",
    "Phone not receiving calls",
    "Monitor displaying strange colors",
    "Need access to shared drive",
    "Computer running very slow",
    "Microsoft Office not launching",
    "Website not loading",
    "Need new hardware",
    "Computer making strange noise",
    "WiFi connection dropping",
    "Need to restore deleted files",
    "Email attachments not opening",
    "Need help with video conference",
    "Can't save documents to network drive",
    "Need to set up new employee"
)

# Sample descriptions
$descriptions = @(
    "I've been trying to access this resource since this morning but keep getting an error.",
    "This issue started yesterday and is preventing me from completing my work.",
    "I need this resolved urgently as it's affecting a critical business process.",
    "I've tried restarting my computer but the problem persists.",
    "This is affecting my entire team and we need it fixed as soon as possible.",
    "I've experienced this issue intermittently for the past week.",
    "I need this software for a presentation tomorrow morning.",
    "I've checked all the usual troubleshooting steps but nothing works.",
    "This is a recurring issue that needs a permanent fix.",
    "I need assistance setting up a new system for a client demo."
)

# Get authentication token
Write-Host "Authenticating..." -ForegroundColor Yellow
try {
    $authResponse = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body ($credentials | ConvertTo-Json) -ContentType "application/json"
    $token = $authResponse.token
    Write-Host "Authentication successful!" -ForegroundColor Green
}
catch {
    Write-Host "Authentication failed: $_" -ForegroundColor Red
    exit
}
