# W500_SLICE_18

- **slot:** 18
- **file:** `D:/Prop/apps/web/src/pages/LiveCopyPage.tsx`
- **angle:** DealRequestByGroup 90-day timeout without chunking
- **read:** full file (8 lines) via `read_file`; grep on this file for `DealRequestByGroup|90.?day|timeout|chunk` returned no matches
- **verdict:** PASS

## Evidence quotes

`LiveCopyPage` is an 8-line static stub. It has **zero imports**, **zero hooks**, **zero `fetch`/`client.get`**, and **zero Manager API surface**. The entire module is:

```1:8:D:/Prop/apps/web/src/pages/LiveCopyPage.tsx
export default function LiveCopyPage() {
  return (
    <div className="space-y-3">
      <h1 className="text-2xl font-semibold text-white">Live copy portfolio</h1>
      <p className="text-amber-300 text-sm">REAL_COPY_EXECUTION_ENABLED is false. This page will stay empty until go-live gates pass.</p>
    </div>
  );
}
```

This file does not contain:

- `DealRequestByGroup` / `GetGroupDealsAsync` / `IMt5BulkDealReader`
- `AddDays(-90)` or any `from`/`to` deal window
- paging, chunking, `DealRequestPage`, or timeout handling
- any broker I/O, ingest, or reconstruction call

The 90-day unchunked group-deal request lives **elsewhere** (out of this slice’s file):

```32:33:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);
```

```49:54:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
```

```240:251:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var arr = _manager!.DealCreateArray();
            try
            {
                var res = _manager.DealRequestByGroup(group, from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds(), arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    throw new InvalidOperationException($"{BrokerCode} DealRequestByGroup {group} failed: {res}");
```

Those backend sites pass the full 90-day window in **one** `DealRequestByGroup` with no day/page chunk. That is **not** this React page. Slot 18 does not re-score the ingest path.

## No-loss implication

`LiveCopyPage` cannot issue `DealRequestByGroup`, cannot hold a 90-day unchunked Manager call until timeout, and cannot drop or duplicate deals from a truncated group dump. It cannot send live copy orders: the only copy-gate text is a **literal** “REAL_COPY_EXECUTION_ENABLED is false,” not a live flag read, and there is no order button. Slot 18 therefore has **no 90-day DealRequestByGroup timeout / no-chunk capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read; the angle (DealRequestByGroup 90-day timeout without chunking) is absent by construction, not by skipped review.
