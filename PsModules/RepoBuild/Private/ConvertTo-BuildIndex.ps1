function ConvertTo-BuildIndex
{
<#
.SYNOPSIS
    Converts a TimeStamp into a build index

.DESCRIPTION
    The algorithm used is the same as the package published. The resulting index is a 32bit value that
    is a combination of the number of days since a fixed point (Upper 16 bits) and the number of seconds since
    midnight (on the day of the input time stamp) divided by 2 {Lower 16 bits)
#>
    param(
        [Parameter(Mandatory=$true, ValueFromPipeLine)]
        [DateTime]$timeStamp
    )

    $timeStamp = $timeStamp.ToUniversalTime()
    $midnightTodayUtc = [DateTime]::new($timeStamp.Year, $timeStamp.Month, $timeStamp.Day, 0, 0, 0, [DateTimeKind]::Utc)
    $baseDate = [DateTime]::new(2000, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)
    $buildNumber = ([Uint32]($timeStamp - $baseDate).Days) -shl 16
    $buildNUmber += [UInt16](($timeStamp - $midnightTodayUtc).TotalSeconds / 2)
    return $buildNumber
}
