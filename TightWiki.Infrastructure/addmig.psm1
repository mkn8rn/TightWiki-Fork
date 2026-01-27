function Resolve-ContextName {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Context
    )

    switch -Regex ($Context) {
        '^Identity$' { return 'IdentityDbContext' }
        '^Wiki$'     { return 'WikiDbContext' }
        default      { return $Context }
    }
}

function Add-Mig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Target,

        [Parameter(Mandatory = $true, Position = 1)]
        [string]$Name
    )

    $contextName = Resolve-ContextName $Target

    switch ($contextName) {
        'IdentityDbContext' { Add-Migration $Name -Context $contextName -OutputDir 'Migrations/Identity'; break }
        'WikiDbContext'     { Add-Migration $Name -Context $contextName -OutputDir 'Migrations/Wiki'; break }
        default             { Add-Migration $Name -Context $contextName; break }
    }
}

Set-Alias addmig Add-Mig

function Up-Db {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Target
    )

    $contextName = Resolve-ContextName $Target
    Update-Database -Context $contextName
}

Set-Alias updb Up-Db
