# W500_SLICE_68

- **slot:** 68
- **file:** `D:/Prop/apps/web/src/pages/LiveCopyPage.tsx`
- **angle:** DealRequestByGroup 90-day timeout without chunking
- **read:** full assigned file (8 lines) via `read_file`; grep on `D:/Prop/apps/web` for `LiveCopy|DealRequest` hit only this stub plus the `/live` route bind in `App.tsx`; repo grep located `DealRequestByGroup` only in `NativeMt5BrokerConnector.cs` and the 90-day host window in `LiveIngestHostedService.cs`
- **verdict:** PASS

## Evidence quotes

`LiveCopyPage` is an 8-line static stub. It has **zero imports**, **zero hooks**, **zero `fetch` / `client.get`**, and **zero Manager API surface**. The entire module is:

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

The only product reference to this component is a route mount. The page is rendered; it does not fetch:

```11:11:D:/Prop/apps/web/src/App.tsx
import LiveCopyPage from './pages/LiveCopyPage';
```

```32:32:D:/Prop/apps/web/src/App.tsx
        <Route path="live" element={<LiveCopyPage />} />
```

This file does not contain:

- `DealRequestByGroup` / `GetGroupDealsAsync` / `IMt5BulkDealReader`
- `AddDays(-90)` or any `from`/`to` deal window
- paging, chunking, `DealRequestPage`, `Windows(`, or timeout handling
- any broker I/O, ingest, reconstruction, or copy-order call
- a live read of `REAL_COPY_EXECUTION_ENABLED` (the string is a **literal** in JSX)

The 90-day group-deal ingest lives **elsewhere** (cited so the empty-PASS is not a miss; **not re-scored** by this slot):

```32:33:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);
```

```64:69:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
```

```296:316:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var all = new List<Mt5DealDto>();
            foreach (var (start, end) in Windows(from, to))
            {
                var arr = _manager!.DealCreateArray();
                try
                {
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
                    if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                        throw new InvalidOperationException(Describe(res, $"{BrokerCode} DealRequestByGroup {group}"));
                    all.AddRange(ReadDeals(arr));
                }
                finally { arr.Release(); }
            }

            return all;
        }
    }
```

```355:366:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Windows(DateTimeOffset from, DateTimeOffset to)
    {
        var cursor = from;
        while (cursor < to)
        {
            var end = cursor.AddDays(14);
            if (end > to)
                end = to;
            yield return (cursor, end);
            cursor = end;
        }
    }
```

Those sites own the host 90-day window and the Manager `DealRequestByGroup` loop (currently split into 14-day `Windows()` slices; still no `DealRequestPage`). Slot 68 does not re-score the ingest path.

## No-loss implication

`LiveCopyPage` cannot issue `DealRequestByGroup`, cannot hold a 90-day (or 14-day) Manager call until timeout, and cannot drop or duplicate deals from a truncated group dump. It cannot send live copy orders: the only copy-gate text is a **literal** “REAL_COPY_EXECUTION_ENABLED is false,” not a live flag read, and there is no order button. Slot 68 therefore has **no 90-day DealRequestByGroup timeout / no-chunk capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read; the angle (DealRequestByGroup 90-day timeout without chunking) is absent by construction, not by skipped review.
