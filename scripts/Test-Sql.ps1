$ErrorActionPreference = 'Stop'
$previousSqlTestConnection = $env:INVENTORY_TEST_SQL_CONNECTION
try {
    if (-not $env:INVENTORY_TEST_SQL_CONNECTION) {
        $envFile = Join-Path $PSScriptRoot '../.env'
        $passwordLine = Get-Content -LiteralPath $envFile | Where-Object { $_ -match '^\s*SQL_PASSWORD=' } | Select-Object -First 1
        if (-not $passwordLine) { throw 'Set INVENTORY_TEST_SQL_CONNECTION or SQL_PASSWORD in the local .env file.' }
        $sqlTestConnection = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $sqlTestConnection['Data Source'] = 'localhost,1433'
        $sqlTestConnection['Initial Catalog'] = 'InventoryManagement'
        $sqlTestConnection['User ID'] = 'sa'
        $sqlTestConnection['Password'] = $passwordLine.Substring($passwordLine.IndexOf('=') + 1).Trim().Trim('"').Trim("'")
        $sqlTestConnection['TrustServerCertificate'] = $true
        $sqlTestConnection['Connect Timeout'] = 10
        $env:INVENTORY_TEST_SQL_CONNECTION = $sqlTestConnection.ConnectionString
    }
    $solution = Join-Path $PSScriptRoot '../InventoryManagement.sln'
    dotnet test $solution --no-restore --nologo -m:1 -v:minimal
    $testExitCode = $LASTEXITCODE
} finally {
    $env:INVENTORY_TEST_SQL_CONNECTION = $previousSqlTestConnection
}
exit $testExitCode
