# =============================================================================
# Joveler.FileMagician Build/Publish Powershell Script
# =============================================================================
# Intended to run on both Powershell (.NET Framework) and Powershell Core (.NET).
#
# On CI, use:
#    .\BinaryPublish.ps1 -noclean
# On Release, use:
#    .\BinaryPublish.ps1

# -----------------------------------------------------------------------------
# Script parameters & banner
# -----------------------------------------------------------------------------
param (
    [switch]$nightly = $false,
    [switch]$noclean = $false
)

$DistName = "Joveler.FileMagician.Cli"

# Banner
Write-Host "[*] Publishing ${DistName} binaries..." -ForegroundColor Cyan

# -----------------------------------------------------------------------------
# Publish mode/arch (Available & Activated)
# -----------------------------------------------------------------------------
# Available publish modes
enum PublishModes
{
    # Runtime-dependent cross-platform binary
    RuntimeDependent = 0
    # Self-contained
    SelfContained
}

# Activated publish modes & arches
$runModes = @(
    ,@( [PublishModes]::RuntimeDependent, "none" )
    ,@( [PublishModes]::SelfContained, "win-x64" )
    ,@( [PublishModes]::SelfContained, "win-arm64" )
    ,@( [PublishModes]::SelfContained, "linux-x64" )
    ,@( [PublishModes]::SelfContained, "linux-arm64" )
)

# -----------------------------------------------------------------------------
# Get directory paths & enviroment infomation
# -----------------------------------------------------------------------------
$BaseDir = $PSScriptRoot
$PublishDir = "${BaseDir}\Publish"
$ToolDir = "${PublishDir}\_tools"
# Unfortunately, 7zip does not provide arm64 build of 7za.exe yet. (v21.07)
$SevenZipExe = "${ToolDir}\7za_x64.exe"
# UPX minimizes release size, but many antiviruses definitely hate it.
# $UpxExe = "${ToolDir}\upx_x64.exe"
$Cores = ${Env:NUMBER_OF_PROCESSORS}
Write-Output "Cores = ${Cores}"

# -------------------------------------------------------------------------
# Remove old publish files
# -------------------------------------------------------------------------
Remove-Item "${PublishDir}\Joveler.FileMagician*" -Recurse -ErrorAction SilentlyContinue

# -----------------------------------------------------------------------------
# Clean the solution and restore NuGet packages (if -noclean is not set)
# -----------------------------------------------------------------------------
if ($noclean -eq $false) {
    Push-Location "${BaseDir}"
    Write-Output ""
    Write-Host "[*] Cleaning the solution" -ForegroundColor Yellow
    dotnet clean -c Release -verbosity:minimal
    Write-Output ""
    Write-Host "[*] Restore NuGet packages" -ForegroundColor Yellow
    dotnet restore --force
    Pop-Location
}

# -----------------------------------------------------------------------------
# Iterate each activated PublishMode
# -----------------------------------------------------------------------------
foreach ($runMode in $runModes)
{
    $PublishMode = $runMode[0]
    $PublishRuntimeId = $runMode[1]

    Write-Output ""
    Write-Host "[*] Publish ${DistName} (${PublishMode}, ${PublishRuntimeId})" -ForegroundColor Cyan

    # -------------------------------------------------------------------------
    # Set up publish variables
    # -------------------------------------------------------------------------
    switch ($PublishMode)
    {
        RuntimeDependent
        { 
            $PublishName = "${DistName}_rt"
            $isRuntimeDependent = $true
            Break
        }
        SelfContained
        { 
            $PublishName = "${DistName}_${PublishRuntimeId}"
            $isRuntimeDependent = $false
            if ($PublishArch -eq "") {
                Write-Host "Invalid publish arch [${PublishArch}]" -ForegroundColor Red
                exit 1
            }
            Break
        }
        default
        {
            Write-Host "Invalid publish mode [${PublishMode}]" -ForegroundColor Red
            exit 1
        }
    }
    
    $DestDir = "${PublishDir}\${PublishName}"
    $DestArchive = "${PublishDir}\${PublishName}.7z"

    # -------------------------------------------------------------------------
    # Remove old publish files
    # -------------------------------------------------------------------------
    Remove-Item "${DestDir}" -Recurse -ErrorAction SilentlyContinue
    Remove-Item "${PublishDir}\${PublishName}.7z" -ErrorAction SilentlyContinue

    New-Item "${DestDir}" -ItemType Directory -ErrorAction SilentlyContinue
    New-Item "${DestDir}" -ItemType Directory -ErrorAction SilentlyContinue

    # -------------------------------------------------------------------------
    # Pack Joveler.FileMagician NuGet
    # -------------------------------------------------------------------------
    Push-Location "${BaseDir}"
    dotnet build -c Release
    dotnet pack -c Release -o "${PublishDir}" Joveler.FileMagician
    Pop-Location

    # -------------------------------------------------------------------------
    # Publish Joveler.FileMagician.Cli
    # -------------------------------------------------------------------------
    Push-Location "${BaseDir}"
    Write-Output ""
    Write-Host "[*] Build ${DistName}" -ForegroundColor Yellow
    if ($isRuntimeDependent -eq $true) {
        dotnet publish -c Release -o "${DestDir}" Joveler.FileMagician.Cli
    } else {
        dotnet publish -c Release -r "${PublishRuntimeId}" -p:PublishTrimmed=true --self-contained -o "${DestDir}" Joveler.FileMagician.Cli
    }
    Pop-Location

    # -------------------------------------------------------------------------
    # Handle native binaries
    # -------------------------------------------------------------------------
    if ($isRuntimeDependent -eq $false) {
        # Flatten the location of native libraries
        Copy-Item "${DestDir}\runtimes\${PublishRuntimeId}\native\*" -Destination "${DestDir}"
        Remove-Item "${DestDir}\runtimes" -Recurse
    }

    # -------------------------------------------------------------------------
    # Delete unnecessary files
    # -------------------------------------------------------------------------
    Remove-Item "${DestDir}\*.pdb" -ErrorAction SilentlyContinue
    Remove-Item "${DestDir}\*.xml" -ErrorAction SilentlyContinue
    
    # -------------------------------------------------------------------------
    # Copy LICENSE files
    # -------------------------------------------------------------------------
    Copy-Item "${BaseDir}\LICENSE" "${DestDir}"
    Copy-Item "${BaseDir}\LICENSE.BSD-2" "${DestDir}"
    if (${PublishRuntimeId}.StartsWith("win-"))
    {
        Copy-Item "${BaseDir}\LICENSE.LGPLv2.1" "${DestDir}"
    }

    # -------------------------------------------------------------------------
    # Create release 7z archive
    # -------------------------------------------------------------------------
    Write-Output ""
    Write-Host "[*] Create ${PublishMode} archive" -ForegroundColor Yellow
    Push-Location "${PublishDir}"
    & "${SevenZipExe}" a "-mmt=${Cores}" "${PublishName}.7z" ".\${PublishName}\*"
    Pop-Location
}
