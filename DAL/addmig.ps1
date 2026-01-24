param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Target,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$Name
)

$ErrorActionPreference = 'Stop'

switch -Regex ($Target) {
    '^Identity$' { Add-Migration $Name -Context IdentityDbContext -OutputDir 'Migrations/Identity'; break }
    '^Wiki$'     { Add-Migration $Name -Context WikiDbContext -OutputDir 'Migrations/Wiki'; break }
    default      { Add-Migration $Name -Context $Target; break }
}
