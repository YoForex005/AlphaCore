export default function ShadowPortfolioPage() {
  return (
    <div className="space-y-3">
      <h1 className="text-2xl font-semibold text-white">Shadow portfolio</h1>
      <p className="text-gray-400 text-sm">
        Shadow fills use the cTrader QUOTE session, not source MT5 ticks. Open/increase intents expire when stale.
        Live NewOrderSingle remains disabled.
      </p>
      <div className="border border-gray-800 rounded p-4 text-gray-300 text-sm">
        Demo seed reconstructs and scores traders. Shadow orders are created only after a CopyIntent is approved for SHADOW state.
      </div>
    </div>
  );
}
