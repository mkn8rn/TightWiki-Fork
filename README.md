# TightWiki-Fork

This project is based on TightWiki (MIT). Original code remains MIT licensed; my modifications are AGPL-3.0.
Link to the original: https://github.com/NTDLS/TightWiki

Personal objectives are to migrate this to PostgreSQL + EFC with a Traditional Architecture paradigm. Why? I don't know. I decided it would be a fun idea. I'm also privately using it as an example of refactoring a project to a new tech stack and architecture paradigm.
Currently WiP, do not use. I'm still figuring out the best way to go about this. The repositories are currently a hackjobbed abomination and will likely be removed entirely, I just needed to get something working in Postgres first.

## EF Core migrations (DAL)

In Visual Studio Package Manager Console set Default project to `DAL`, then run: `Import-Module (Join-Path (Get-Location) 'DAL\addmig.psm1') -Force`.
Use `addmig Identity <MigrationName>` / `addmig Wiki <MigrationName>` to create migrations and `updb Identity` / `updb Wiki` to apply them.
Any other value is passed through as the exact EF `-Context` name.
