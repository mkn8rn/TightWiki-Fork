# TightWiki-Fork

This project is based on TightWiki (MIT). Original code remains MIT licensed; my modifications are AGPL-3.0.
Link to the original: https://github.com/NTDLS/TightWiki

## EF Core migrations (DAL)

In Visual Studio Package Manager Console set Default project to `DAL`, then run: `Import-Module (Join-Path (Get-Location) 'DAL\addmig.psm1') -Force`.
Use `addmig Identity <MigrationName>` / `addmig Wiki <MigrationName>` to create migrations and `updb Identity` / `updb Wiki` to apply them.
Any other value is passed through as the exact EF `-Context` name.