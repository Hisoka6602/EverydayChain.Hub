param(
    [string]$RootDirectory = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

function Read-FileLinesUtf8 {
    param(
        [string]$FilePath
    )

    return [System.IO.File]::ReadAllLines($FilePath, [System.Text.Encoding]::UTF8)
}

function Write-FileLinesUtf8 {
    param(
        [string]$FilePath,
        [string[]]$Lines
    )

    [System.IO.File]::WriteAllLines($FilePath, $Lines, [System.Text.Encoding]::UTF8)
}

function Get-ClassSummaryMap {
    $map = @{}
    $map['ApiResponse'] = '表示接口统一响应包装，包含是否成功、提示消息和业务数据。'
    $map['BoxTrackingQueryRequest'] = '表示箱子追踪查询条件。'
    $map['BusinessTaskProjectionBackfillPreviewRequest'] = '表示业务任务补投影预览请求参数。'
    $map['BusinessTaskProjectionBackfillRequest'] = '表示业务任务补投影执行请求参数。'
    $map['BusinessTaskQueryRequest'] = '表示业务任务、异常件或回流件明细查询条件。'
    $map['BusinessTaskSeedRequest'] = '表示业务任务手工补数请求参数。'
    $map['ChuteResolveRequest'] = '表示格口解析请求参数。'
    $map['CurrentWaveQueryRequest'] = '表示当前自动识别波次查询条件。'
    $map['DockDashboardQueryRequest'] = '表示码头看板查询条件。'
    $map['DropFeedbackRequest'] = '表示落格回传请求参数。'
    $map['ExportCatalogQueryRequest'] = '表示导出中心目录查询条件。'
    $map['GlobalDashboardQueryRequest'] = '表示总看板查询条件。'
    $map['ManualSyncRequest'] = '表示手工同步触发请求参数。'
    $map['RecirculationSummaryQueryRequest'] = '表示回流汇总查询条件。'
    $map['ScanUploadRequest'] = '表示扫描上传请求参数。'
    $map['SortingReportQueryRequest'] = '表示分拣报表查询条件。'
    $map['WaveCleanupRequest'] = '表示波次清理请求参数。'
    $map['WaveDetailQueryRequest'] = '表示波次明细查询条件。'
    $map['WaveListQueryRequest'] = '表示波次列表查询条件。'
    $map['WaveOptionsQueryRequest'] = '表示波次下拉选项查询条件。'
    $map['WaveSummaryQueryRequest'] = '表示单个波次汇总查询条件。'
    $map['WaveZoneQueryRequest'] = '表示波次分区汇总查询条件。'
    $map['BoxTrackingItemResponse'] = '表示箱子追踪结果中的单条扫描与匹配记录。'
    $map['BoxTrackingResponse'] = '表示箱子追踪分页查询结果。'
    $map['BusinessTaskItemResponse'] = '表示业务任务明细中的单条记录。'
    $map['BusinessTaskProjectionBackfillPreviewResponse'] = '表示业务任务补投影预览结果。'
    $map['BusinessTaskProjectionBackfillPreviewTableResponse'] = '表示单张表的业务任务补投影预览统计结果。'
    $map['BusinessTaskProjectionBackfillResponse'] = '表示业务任务补投影执行结果。'
    $map['BusinessTaskProjectionBackfillTableResponse'] = '表示单张表的业务任务补投影执行结果。'
    $map['BusinessTaskQueryResponse'] = '表示业务任务分页查询结果。'
    $map['BusinessTaskSeedResponse'] = '表示业务任务手工补数执行结果。'
    $map['ChuteResolveResponse'] = '表示格口解析结果。'
    $map['CurrentWaveResponse'] = '表示当前自动识别波次结果。'
    $map['DockDashboardResponse'] = '表示码头看板查询结果。'
    $map['DockDashboardSummaryResponse'] = '表示单个码头的看板汇总结果。'
    $map['DropFeedbackResponse'] = '表示落格回传处理结果。'
    $map['ExportCatalogItemResponse'] = '表示导出中心中的单个导出项。'
    $map['ExportCatalogResponse'] = '表示导出中心目录查询结果。'
    $map['GlobalDashboardResponse'] = '表示总看板汇总结果。'
    $map['ManualSyncBatchResponse'] = '表示单个同步批次的执行结果。'
    $map['ManualSyncResponse'] = '表示手工同步整体执行结果。'
    $map['RecirculationSummaryResponse'] = '表示回流汇总查询结果。'
    $map['RecirculationSummaryRowResponse'] = '表示回流汇总中的单行统计结果。'
    $map['ScanUploadResponse'] = '表示单个条码的扫描受理结果。'
    $map['SortingReportResponse'] = '表示分拣报表查询结果。'
    $map['SortingReportRowResponse'] = '表示分拣报表中的单个码头统计结果。'
    $map['WaveCleanupQueryResponse'] = '表示波次清理查询结果。'
    $map['WaveCleanupResponse'] = '表示波次清理执行结果。'
    $map['WaveCleanupWaveItemResponse'] = '表示待清理波次的单条汇总记录。'
    $map['WaveDashboardSummaryResponse'] = '表示总看板中的单个波次汇总结果。'
    $map['WaveDetailItemResponse'] = '表示波次明细中的单条业务任务记录。'
    $map['WaveDetailResponse'] = '表示波次明细查询结果。'
    $map['WaveListItemResponse'] = '表示波次列表中的单个波次汇总记录。'
    $map['WaveListResponse'] = '表示波次列表查询结果。'
    $map['WaveOptionItemResponse'] = '表示波次下拉选项中的单个候选波次。'
    $map['WaveOptionsResponse'] = '表示波次下拉选项查询结果。'
    $map['WaveSummaryResponse'] = '表示单个波次的汇总结果。'
    $map['WaveZoneResponse'] = '表示波次分区汇总查询结果。'
    $map['WaveZoneSummaryResponse'] = '表示单个波次分区的统计结果。'
    return $map
}

function Get-PropertySummaryMap {
    $map = @{}
    $map['ActualChuteCode'] = '表示实际落格编码。'
    $map['Barcode'] = '表示箱码或业务条码。'
    $map['BarcodeType'] = '表示当前条码识别出的条码类型。'
    $map['Barcodes'] = '表示本次请求提交的条码列表。'
    $map['BatchId'] = '表示同步批次标识。'
    $map['BatchSize'] = '表示批量处理时每批写入的记录数。'
    $map['BoxId'] = '表示箱号或箱码标识。'
    $map['CandidateCount'] = '表示满足条件的候选记录数量。'
    $map['Chute'] = '表示落格或统计对应的格口编码。'
    $map['ChuteCode'] = '表示格口编码。'
    $map['CleanedCount'] = '表示实际完成清理的任务数量。'
    $map['Content'] = '表示导出项展示名称或内容摘要。'
    $map['CreatedAt'] = '表示记录创建时间（本地时间）。'
    $map['CreatedTimeLocal'] = '表示记录创建时间（本地时间）。'
    $map['Data'] = '表示接口返回的业务数据内容。'
    $map['DataDownloadProgressPercent'] = '表示源数据下载进度百分比。'
    $map['DataWritebackProgressPercent'] = '表示源数据回传进度百分比。'
    $map['DeduplicatedCount'] = '表示去重阶段剔除的重复记录数量。'
    $map['DeleteCount'] = '表示本次删除处理的记录数量。'
    $map['DeviceCode'] = '表示发起扫描的设备编码。'
    $map['DockCode'] = '表示码头编码。'
    $map['DockSummaries'] = '表示各码头维度的统计结果列表。'
    $map['EndTimeLocal'] = '表示查询或统计结束时间（本地时间）。'
    $map['Endpoint'] = '表示对应的导出接口路径。'
    $map['ErrorMessage'] = '表示批次执行失败时的错误说明。'
    $map['ExceptionCount'] = '表示异常件数量。'
    $map['FailedBatchCount'] = '表示执行失败的批次数量。'
    $map['FailureReason'] = '表示处理失败、未命中或被拒绝的原因说明。'
    $map['FilteredEmptyCount'] = '表示因空值被过滤掉的记录数量。'
    $map['Format'] = '表示导出文件格式。'
    $map['FullCaseSortedCount'] = '表示整件已分拣数量。'
    $map['FullCaseSortedProgressPercent'] = '表示整件分拣进度百分比。'
    $map['FullCaseTotalCount'] = '表示整件总数量。'
    $map['FullCaseUnsortedCount'] = '表示整件待分拣数量。'
    $map['FullRatioPercent'] = '表示整件数量在当前波次中的占比百分比。'
    $map['FullTotal'] = '表示整件任务总数量。'
    $map['GeneratedTimeLocal'] = '表示导出目录生成时间（本地时间）。'
    $map['HasMore'] = '表示当前结果后续是否仍有更多数据。'
    $map['HeightMm'] = '表示箱体高度，单位为毫米。'
    $map['IdentifiedCount'] = '表示已识别出待清理任务的数量。'
    $map['InsertCount'] = '表示本次新增写入的记录数量。'
    $map['InsertedBarcodes'] = '表示本次成功补写到本地库的条码列表。'
    $map['InsertedCount'] = '表示成功插入本地库的记录数量。'
    $map['IsAccepted'] = '表示当前请求是否已经被系统受理。'
    $map['IsDryRun'] = '表示当前结果是否来自预演执行。'
    $map['IsException'] = '表示任务是否被判定为异常件。'
    $map['IsMatched'] = '表示扫描条码是否成功匹配到业务任务。'
    $map['IsRecirculated'] = '表示任务是否发生过回流。'
    $map['IsResolved'] = '表示当前条码是否成功解析出目标格口。'
    $map['IsSuccess'] = '表示当前接口调用或回传结果是否处理成功。'
    $map['Items'] = '表示当前结果包含的明细列表。'
    $map['Key'] = '表示导出项唯一标识。'
    $map['LastCreatedTimeLocal'] = '表示游标翻页请求使用的最后创建时间锚点。'
    $map['LastId'] = '表示游标翻页请求使用的最后主键锚点。'
    $map['LatestSyncTimeLocal'] = '表示最近一次同步完成时间（本地时间）。'
    $map['LengthMm'] = '表示箱体长度，单位为毫米。'
    $map['MaxCount'] = '表示单次允许处理的最大记录数。'
    $map['Message'] = '表示本次接口处理返回的提示信息。'
    $map['MissingOrderIdCount'] = '表示缺少订单标识的候选记录数量。'
    $map['MissingPickLocationCount'] = '表示缺少拣货位的候选记录数量。'
    $map['MissingProductCodeCount'] = '表示缺少商品编码的候选记录数量。'
    $map['MissingRemoteCount'] = '表示在远端未找到对应数据的记录数量。'
    $map['MissingStoreIdCount'] = '表示缺少门店标识的候选记录数量。'
    $map['MissingStoreNameCount'] = '表示缺少门店名称的候选记录数量。'
    $map['NextLastCreatedTimeLocal'] = '表示游标翻页下一页使用的创建时间锚点。'
    $map['NextLastId'] = '表示游标翻页下一页使用的主键锚点。'
    $map['OrderId'] = '表示订单标识。'
    $map['PackageTotal'] = '表示该波次对应的总包裹数量。'
    $map['PageNumber'] = '表示分页页码，从 1 开始计数。'
    $map['PageSize'] = '表示每页返回的记录条数。'
    $map['PaginationMode'] = '表示当前结果采用的分页模式。'
    $map['PickLocation'] = '表示拣货位编码。'
    $map['ProcessedTableCount'] = '表示本次处理涉及的数据表数量。'
    $map['ProductCode'] = '表示商品编码。'
    $map['ProjectedCount'] = '表示成功完成投影补全的记录数量。'
    $map['ReadCount'] = '表示本次读取的记录数量。'
    $map['RecognitionRatePercent'] = '表示读码识别率百分比。'
    $map['RecirculatedCount'] = '表示回流数量。'
    $map['Reflow'] = '表示回流次数。'
    $map['Remark'] = '表示备注信息。'
    $map['RemoteRowCount'] = '表示从远端成功读取到的记录数量。'
    $map['RequestedCount'] = '表示请求提交的记录数量。'
    $map['Rows'] = '表示当前结果包含的统计行列表。'
    $map['ScanTimeLocal'] = '表示扫描发生时间（本地时间）。'
    $map['ScannedAt'] = '表示条码扫描完成时间（本地时间）。'
    $map['Scanner'] = '表示扫描设备或扫描人员标识。'
    $map['Scope'] = '表示导出项所属业务范围。'
    $map['SelectedChuteCode'] = '表示本次统计实际使用的格口编码。'
    $map['SelectedDockCode'] = '表示本次统计实际使用的码头编码。'
    $map['SelectedWaveCode'] = '表示本次统计实际使用的波次号。'
    $map['SkipCount'] = '表示本次因重复或无变更而跳过的记录数量。'
    $map['SkippedBarcodes'] = '表示本次被跳过的条码列表。'
    $map['SkippedExistingCount'] = '表示因本地已存在而跳过的记录数量。'
    $map['SortOrder'] = '表示统计结果的排序方式。'
    $map['SourceType'] = '表示任务来源类型。'
    $map['SplitRatioPercent'] = '表示拆零数量在当前波次中的占比百分比。'
    $map['SplitSortedCount'] = '表示拆零已分拣数量。'
    $map['SplitSortedProgressPercent'] = '表示拆零分拣进度百分比。'
    $map['SplitTotal'] = '表示拆零任务总数量。'
    $map['SplitTotalCount'] = '表示拆零总数量。'
    $map['SplitUnsortedCount'] = '表示拆零待分拣数量。'
    $map['StartTimeLocal'] = '表示查询或统计开始时间（本地时间）。'
    $map['Status'] = '表示当前任务、波次或批次的业务状态。'
    $map['StoreId'] = '表示门店标识。'
    $map['StoreName'] = '表示门店名称。'
    $map['SuccessBatchCount'] = '表示执行成功的批次数量。'
    $map['SortedCount'] = '表示已完成分拣的数量。'
    $map['SortedProgressPercent'] = '表示当前维度的分拣进度百分比。'
    $map['TableCode'] = '表示同步表或业务表编码。'
    $map['Tables'] = '表示按数据表拆分的处理结果列表。'
    $map['TargetChuteCode'] = '表示系统计算出的目标格口编码。'
    $map['TargetTableName'] = '表示补数写入的目标表名。'
    $map['TaskCode'] = '表示业务任务编码。'
    $map['TotalBatchCount'] = '表示本次同步执行产生的批次数量。'
    $map['TotalCount'] = '表示统计范围内的总数量。'
    $map['TotalSortedProgressPercent'] = '表示整体分拣进度百分比。'
    $map['TotalVolumeMm3'] = '表示累计体积，单位为立方毫米。'
    $map['TotalWeightGram'] = '表示累计重量，单位为克。'
    $map['TraceId'] = '表示调用链路跟踪标识。'
    $map['TriggeredAtLocal'] = '表示本次手工同步触发时间（本地时间）。'
    $map['Type'] = '表示导出项类型。'
    $map['UnsortedCount'] = '表示尚未完成分拣的数量。'
    $map['UpdatedAt'] = '表示记录最后更新时间（本地时间）。'
    $map['UpdatedCount'] = '表示本次实际更新的记录数量。'
    $map['UpdateCount'] = '表示本次更新写入的记录数量。'
    $map['VolumeMm3'] = '表示箱体体积，单位为立方毫米。'
    $map['WaveCode'] = '表示波次号。'
    $map['WaveId'] = '表示波次号。'
    $map['WaveNo'] = '表示波次号。'
    $map['WaveOptions'] = '表示可供前端筛选的波次列表。'
    $map['WaveRemark'] = '表示波次备注。'
    $map['WaveSummaries'] = '表示各波次维度的汇总结果列表。'
    $map['WeightGram'] = '表示箱体重量，单位为克。'
    $map['WidthMm'] = '表示箱体宽度，单位为毫米。'
    $map['WorkingArea'] = '表示拆零作业所属的工作区域。'
    $map['ZoneCode'] = '表示波次分区编码。'
    $map['ZoneName'] = '表示波次分区名称。'
    $map['Zones'] = '表示当前波次下各分区的统计结果列表。'
    return $map
}

function Update-ContractComments {
    param(
        [string]$FilePath,
        [hashtable]$ClassSummaryMap,
        [hashtable]$PropertySummaryMap
    )

    $lines = Read-FileLinesUtf8 -FilePath $FilePath
    $updated = $false

    for ($index = 0; $index -lt $lines.Length; $index++) {
        $lineText = $lines[$index]

        $classMatch = [regex]::Match($lineText, 'public\s+sealed\s+class\s+([A-Za-z_][A-Za-z0-9_]*)')
        if ($classMatch.Success) {
            $className = $classMatch.Groups[1].Value
            if ($ClassSummaryMap.ContainsKey($className) -and $index -ge 2 -and $lines[$index - 1].Trim() -eq '/// </summary>') {
                $indent = ([regex]::Match($lines[$index - 2], '^\s*')).Value
                $lines[$index - 2] = "$indent/// $($ClassSummaryMap[$className])"
                $updated = $true
            }
        }

        $propertyMatch = [regex]::Match($lineText, 'public\s+[A-Za-z_\<\>\?\[\],\s]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;\s*set;\s*\}')
        if ($propertyMatch.Success) {
            $propertyName = $propertyMatch.Groups[1].Value
            if ($PropertySummaryMap.ContainsKey($propertyName) -and $index -ge 2 -and $lines[$index - 1].Trim() -eq '/// </summary>') {
                $indent = ([regex]::Match($lines[$index - 2], '^\s*')).Value
                $lines[$index - 2] = "$indent/// $($PropertySummaryMap[$propertyName])"
                $updated = $true
            }
        }
    }

    if ($updated) {
        Write-FileLinesUtf8 -FilePath $FilePath -Lines $lines
    }
}

$classSummaryMap = Get-ClassSummaryMap
$propertySummaryMap = Get-PropertySummaryMap
$contractDirectories = @(
    (Join-Path $RootDirectory 'EverydayChain.Hub.Host\Contracts\Requests'),
    (Join-Path $RootDirectory 'EverydayChain.Hub.Host\Contracts\Responses')
)

foreach ($contractDirectory in $contractDirectories) {
    $contractFiles = Get-ChildItem -LiteralPath $contractDirectory -File -Filter '*.cs'
    foreach ($contractFile in $contractFiles) {
        Update-ContractComments -FilePath $contractFile.FullName -ClassSummaryMap $classSummaryMap -PropertySummaryMap $propertySummaryMap
    }
}

