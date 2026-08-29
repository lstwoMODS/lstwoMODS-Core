$gamePath = Get-Content "$PSScriptRoot\game_path.txt" -Raw | ForEach-Object { $_.Trim() }
$publishDir = "$PSScriptRoot\bin\Release\net472\publish"
$overlayPublishDir = "$PSScriptRoot\lstwoMODS_Overlay\bin\Release\net472\win-x64\publish"
$pluginsDir = "$gamePath\BepInEx\plugins\lstwoMODS"
$overlayDir = "$pluginsDir\Overlay"

Copy-Item "$publishDir\lstwoMODS_Core.dll" -Destination $pluginsDir -Force
Copy-Item "$publishDir\lstwoMODS.ImGui.Shared.dll" -Destination $pluginsDir -Force
Copy-Item "$publishDir\DynamicExpresso.Core.dll" -Destination $pluginsDir -Force

# Third-party license notices, one folder per shipped package.
$licensesDir = "$pluginsDir\licenses"
if (-not (Test-Path $licensesDir)) { New-Item -ItemType Directory -Path $licensesDir -Force | Out-Null }
Copy-Item "$PSScriptRoot\licenses\*" -Destination $licensesDir -Recurse -Force

# imgui.ini is the user's saved window layout  running the overlay standalone (test
# harnesses) drops one into the publish dir; copying it would clobber the game's layout.
Copy-Item "$overlayPublishDir\*" -Destination $overlayDir -Recurse -Force -Exclude "imgui.ini"

Start-Process "$gamePath\Wobbly Life.exe"
