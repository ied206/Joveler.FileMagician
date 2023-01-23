# =============================================================================
# Joveler.FileMagician Build/Publish Powershell Script
# =============================================================================
# Intended to run on both Powershell (.NET Framework) and Powershell Core (.NET).
#
# On CI, use:
#    .\BinaryPublish.ps1 -nightly -noclean
# On Release, use:
#    .\BinaryPublish.ps1

# -----------------------------------------------------------------------------
# Script parameters & banner
# -----------------------------------------------------------------------------
param (
    [switch]$nightly = $false,
    [switch]$noclean = $false
)

# Is CI Mode?
if ($nightly) {
    $BinaryName = "nightly"
} else {
    $BinaryName = "release"
}

# Banner
Write-Host "[*] Publishing Joveler.FileMagician ${BinaryName} binaries..." -ForegroundColor Cyan

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
# Available publish architectures (.exe arch in runtime-dependent, runtimeId in self-contained)
enum PublishArches
{
    None = 0
    x86
    x64
    arm64
}

# Activated publish modes & arches
$runModes = @(
    ,@( [PublishModes]::RuntimeDependent, [PublishArches]::None )
    ,@( [PublishModes]::SelfContained, [PublishArches]::x64 )
    ,@( [PublishModes]::SelfContained, [PublishArches]::arm64 )
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
    $PublishArch = $runMode[1]

    Write-Output ""
    Write-Host "[*] Publish Joveler.FileMagician (${BinaryName}, ${PublishMode}, ${PublishArch})" -ForegroundColor Cyan

    # -------------------------------------------------------------------------
    # Set up publish variables
    # -------------------------------------------------------------------------
    switch ($PublishMode)
    {
        RuntimeDependent
        { 
            $LauncherMode = 2 # BUILD_NETCORE_RT_DEPENDENT
            $PublishName = "Joveler.FileMagician-${BinaryName}-rt"
            $isRuntimeDependent = $true
            if ($PublishArch -ne [PublishArches]::None) {
                Write-Host "Invalid publish arch [${PublishArch}]" -ForegroundColor Red
                exit 1
            }
            Break
        }
        SelfContained
        { 
            $LauncherMode = 3 # BUILD_NETCORE_SELF_CONTAINED
            $PublishName = "Joveler.FileMagician-${BinaryName}-sc_${PublishArch}"
            $isRuntimeDependent = $false
            if ($PublishArch -eq [PublishArches]::None) {
                Write-Host "Invalid publish arch [${PublishArch}]" -ForegroundColor Red
                exit 1
            }
            $PublishRuntimeId = "win-${PublishArch}"
            Break
        }
        default
        {
            Write-Host "Invalid publish mode [${PublishMode}]" -ForegroundColor Red
            exit 1
        }
    }
    
    $DestDir = "${PublishDir}\${PublishName}"
    $DestBinDir = "${DestDir}\Binary"

    # -------------------------------------------------------------------------
    # Remove old publish files
    # -------------------------------------------------------------------------
    Remove-Item "${DestDir}" -Recurse -ErrorAction SilentlyContinue
    Remove-Item "${PublishDir}\${PublishName}.7z" -ErrorAction SilentlyContinue

    New-Item "${DestDir}" -ItemType Directory -ErrorAction SilentlyContinue
    New-Item "${DestBinDir}" -ItemType Directory -ErrorAction SilentlyContinue

    # -------------------------------------------------------------------------
    # Pack Joveler.FileMagician NuGet
    # -------------------------------------------------------------------------
    Push-Location "${BaseDir}"
    dotnet build -c Release
    dotnet pack -c Release -o "${BaseDir}" Joveler.FileMagician
    Pop-Location

    # -------------------------------------------------------------------------
    # Publish Joveler.FileMagician.Cli
    # -------------------------------------------------------------------------
    Push-Location "${BaseDir}"
    Write-Output ""
    Write-Host "[*] Build Joveler.FileMagician.Cli" -ForegroundColor Yellow
    if ($isRuntimeDependent -eq $true) {
        dotnet publish -c Release -o "${DestBinDir}" Joveler.FileMagician.Cli
    } else {
        dotnet publish -c Release -r "${PublishRuntimeId}" -p:PublishTrimmed=true --self-contained -o "${DestBinDir}" Joveler.FileMagician.Cli
    }
    Pop-Location

    # -------------------------------------------------------------------------
    # Handle native binaries
    # -------------------------------------------------------------------------
    if ($isRuntimeDependent -eq $true) {
        # PEBakery does not support win-arm, linux, and macOS.
        #Remove-Item "${DestBinDir}\runtimes\linux*" -Recurse
        #Remove-Item "${DestBinDir}\runtimes\alpine*" -Recurse
        #Remove-Item "${DestBinDir}\runtimes\osx*" -Recurse
        #Remove-Item "${DestBinDir}\runtimes\win-arm" -Recurse
    } else {
        # Flatten the location of native libraries
        Copy-Item "${DestBinDir}\runtimes\${PublishRuntimeId}\native\*" -Destination "${DestBinDir}"
        Remove-Item "${DestBinDir}\runtimes" -Recurse
    }

    # -------------------------------------------------------------------------
    # Delete unnecessary files
    # -------------------------------------------------------------------------
    Remove-Item "${DestBinDir}\*.pdb" -ErrorAction SilentlyContinue
    Remove-Item "${DestBinDir}\*.xml" -ErrorAction SilentlyContinue
    Remove-Item "${DestBinDir}\*.db" -ErrorAction SilentlyContinue

    # -------------------------------------------------------------------------
    # Copy LICENSE files
    # -------------------------------------------------------------------------
    Copy-Item "${BaseDir}\LICENSE" "${DestBinDir}"
    Copy-Item "${BaseDir}\LICENSE.GPLv3" "${DestBinDir}"

    # -------------------------------------------------------------------------
    # Create release 7z archive
    # -------------------------------------------------------------------------
    Write-Output ""
    Write-Host "[*] Create ${PublishMode} ${BinaryName} archive" -ForegroundColor Yellow
    Push-Location "${PublishDir}"
    & "${SevenZipExe}" a "-mmt=${Cores}" "${PublishName}.7z" ".\${PublishName}\*"
    Pop-Location
}
