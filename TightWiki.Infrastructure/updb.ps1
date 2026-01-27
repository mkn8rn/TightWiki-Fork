param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Target
)

$ErrorActionPreference = 'Stop'

switch -Regex ($Target) {
    '^Identity$' { Update-Database -Context IdentityDbContext; break }
    '^Wiki$'     { Update-Database -Context WikiDbContext; break }
    default      { Update-Database -Context $Target; break }
}
