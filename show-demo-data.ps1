param(
    [int]$TripId=0,
    [int]$RequestId=0,
    [ValidatePattern('^[A-Za-z0-9_]+$')][string]$Database='TuTucDemo'
)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
$settings=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'appsettings.json') -Raw | ConvertFrom-Json
$connectionSettings=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($settings.ConnectionStrings.Default)
$connectionSettings['Initial Catalog']=$Database
# The terminal helper uses the Windows SqlClient against local LocalDB only.
if($connectionSettings['Data Source'] -like '(localdb)\*'){$connectionSettings['Encrypt']=$false}
$connection=New-Object System.Data.SqlClient.SqlConnection($connectionSettings.ConnectionString)
function Show-Query([string]$Title,[string]$Sql,[int]$Id,[switch]$Details){
    Write-Host "`n$Title" -ForegroundColor Cyan
    $command=$connection.CreateCommand()
    $command.CommandText=$Sql
    [void]$command.Parameters.Add('@id',[System.Data.SqlDbType]::Int)
    $command.Parameters['@id'].Value=$Id
    $table=New-Object System.Data.DataTable
    $reader=$command.ExecuteReader()
    try {$table.Load($reader)} finally {$reader.Dispose();$command.Dispose()}
    if($table.Rows.Count -eq 0){Write-Host '(Khong co du lieu)';return}
    $rows=foreach($row in $table.Rows){$values=[ordered]@{};foreach($column in $table.Columns){$values[$column.ColumnName]=$row[$column.ColumnName]};[pscustomobject]$values}
    if($Details){$rows | Format-List | Out-Host}else{$rows | Format-Table -AutoSize -Wrap | Out-Host}
}
$connection.Open()
try {
    if($TripId -gt 0){
        Show-Query 'KE HOACH - Bang 4.179 / 4.185' 'SELECT Id,Name,Destination,Start,[End],Budget,Status,CONVERT(varchar(18),CONVERT(varbinary(8),Version),1) AS RowVersion FROM Trips WHERE Id=@id' $TripId
        Show-Query 'HOAT DONG - Bang 4.180 / 4.181 / 4.182' 'SELECT Id,Start,[End],SortOrder,Title,Cost,HasTime FROM Activities WHERE TripId=@id ORDER BY CAST(Start AS date),SortOrder,Start,Id' $TripId
    }
    if($RequestId -gt 0){
        Show-Query 'YEU CAU - Bang 4.220 / 4.225 / 4.226' 'SELECT Id,SnapshotName,Status,Date,EndDate,People AS SoLuong,Guests AS SoNguoi,Total,ConditionsVersion,CustomerAcceptedVersion,ProviderAcceptedVersion,CONVERT(varchar(18),CONVERT(varbinary(8),Version),1) AS RowVersion FROM Bookings WHERE Id=@id' $RequestId -Details
        Show-Query 'LICH SU DIEU KIEN - doi chieu ban cu va ban moi' 'SELECT BookingId,VersionNumber,Reason,Snapshot,CreatedAt FROM BookingConditionVersions WHERE BookingId=@id ORDER BY VersionNumber' $RequestId -Details
        Show-Query 'NHAT KY DE XUAT - SK11 - Bang 4.225' 'SELECT EventCode,Action,ActorId,[Before],[After],CreatedAt FROM AuditEntries WHERE Entity=N''Booking'' AND EntityId=CONVERT(nvarchar(20),@id) ORDER BY Id' $RequestId -Details
        Show-Query 'THONG BAO - Bang 4.265' 'SELECT OwnerId,Type,Title,Url,IsRead FROM Notifications WHERE Url IN (N''/Provider/Request/''+CONVERT(nvarchar(20),@id),N''/Marketplace/Request/''+CONVERT(nvarchar(20),@id)) ORDER BY Id' $RequestId
    }
    if($TripId -le 0 -and $RequestId -le 0){Write-Host 'Vi du: .\show-demo-data.ps1 -TripId 1005 -RequestId 7'}
} finally {$connection.Dispose()}

