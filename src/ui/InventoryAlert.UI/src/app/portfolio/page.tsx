'use client'

import { useState, useEffect } from "react";
import Link from "next/link";
import { fetchApi } from "@/lib/api";
import { Toast } from "@/components/Toast";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { TradeModal } from "@/components/TradeModal";
import Pagination from "@/components/ui/Pagination";
import { getErrorMessage } from "@/lib/error-utils";

interface PortfolioPosition {
  symbol: string;
  name: string;
  exchange: string;
  logo: string;
  holdingsCount: number;
  averagePrice: number;
  currentPrice: number;
  marketValue: number;
  totalCost: number;
  totalReturn: number;
  totalReturnPercent: number;
  priceChangePercent: number;
  industry: string;
}

export default function PortfolioPage() {
  const [positions, setPositions] = useState<PortfolioPosition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [toast, setToast] = useState<{message: string, type: 'success' | 'error'} | null>(null);
  const [selectedSymbol, setSelectedSymbol] = useState<string | null>(null);
  const [isTradeModalOpen, setIsTradeModalOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);

  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [summary, setSummary] = useState({
    totalMarketValue: 0,
    totalCost: 0,
    totalGainLoss: 0,
    totalGainLossPercent: 0,
  });
  const pageSize = 10;

  const loadSummary = async () => {
    try {
      const data = await fetchApi('/api/v1/portfolio/positions?pageNumber=1&pageSize=1000');
      const all: PortfolioPosition[] = data.items || [];
      const totalMVal = all.reduce((acc, p) => acc + (p.marketValue || 0), 0);
      const totalC = all.reduce((acc, p) => acc + (p.totalCost || 0), 0);
      const totalGL = totalMVal - totalC;
      const totalGLPercent = totalC > 0 ? (totalGL / totalC) * 100 : 0;
      setSummary({
        totalMarketValue: totalMVal,
        totalCost: totalC,
        totalGainLoss: totalGL,
        totalGainLossPercent: totalGLPercent,
      });
      setTotalItems(data.totalItems ?? all.length);
    } catch {
      // Summary error handled gracefully
    }
  };

  const loadPortfolio = async (targetPage: number) => {
    try {
      setLoading(true);
      const data = await fetchApi(`/api/v1/portfolio/positions?pageNumber=${targetPage}&pageSize=${pageSize}`);
      setPositions(data.items || []);
      setTotalPages(data.totalPages || 1);
      setPage(data.pageNumber || targetPage);
      if (data.totalItems !== undefined) {
        setTotalItems(data.totalItems);
      }
    } catch (err: any) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPortfolio(1);
    loadSummary();
  }, []);

  const handleDelete = async (symbol: string) => {
    try {
      await fetchApi(`/api/v1/portfolio/positions/${symbol}`, { method: 'DELETE' });
      setToast({ message: `Position for ${symbol} removed`, type: 'success' });
      loadPortfolio(page);
      loadSummary();
    } catch (err: any) {
      setToast({ message: getErrorMessage(err), type: 'error' });
    } finally {
      setConfirmDelete(null);
    }
  };

  const handleTradeSuccess = () => {
    setToast({ message: "Trade recorded successfully", type: 'success' });
    loadPortfolio(page);
    loadSummary();
  };

  return (
    <div className="max-w-7xl mx-auto space-y-10">
      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
      
      <ConfirmDialog
        isOpen={!!confirmDelete}
        title="Remove Position"
        message={`Are you sure you want to remove ${confirmDelete} from your portfolio? This will also delete all trade history for this symbol.`}
        confirmText="Remove"
        type="danger"
        onConfirm={() => confirmDelete && handleDelete(confirmDelete)}
        onCancel={() => setConfirmDelete(null)}
      />

      <TradeModal 
        isOpen={isTradeModalOpen}
        onClose={() => setIsTradeModalOpen(false)}
        symbol={selectedSymbol || ""}
        onSuccess={handleTradeSuccess}
      />

      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-6">
        <div>
          <h1 className="text-4xl font-semibold tracking-tight text-zinc-900 dark:text-white uppercase">Portfolio</h1>
          <p className="text-zinc-500 dark:text-zinc-400 mt-2 font-medium">Manage your holdings and track performance.</p>
        </div>
        <button 
          onClick={() => { setSelectedSymbol(null); setIsTradeModalOpen(true); }}
          className="px-8 py-4 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-2xl shadow-xl shadow-blue-500/20 transition-all active:scale-95 uppercase tracking-wider text-xs"
        >
          + New Position
        </button>
      </div>

      {error && (
        <div className="p-4 bg-rose-500/10 border border-rose-500/20 text-rose-500 rounded-2xl text-sm font-semibold text-center">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 p-8 rounded-3xl shadow-sm dark:shadow-none">
          <p className="text-zinc-500 text-xs font-semibold uppercase tracking-wider mb-2">Total Market Value</p>
          <p className="text-4xl font-semibold text-zinc-900 dark:text-white">${summary.totalMarketValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
        </div>
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 p-8 rounded-3xl shadow-sm dark:shadow-none">
          <p className="text-zinc-500 text-xs font-semibold uppercase tracking-wider mb-2">Total Return</p>
          <div className="flex items-baseline gap-3">
            <p className={`text-4xl font-semibold ${summary.totalGainLoss >= 0 ? "text-emerald-500" : "text-rose-500"}`}>
              {summary.totalGainLoss >= 0 ? "+" : ""}${Math.abs(summary.totalGainLoss).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            </p>
            <p className={`text-lg font-bold ${summary.totalGainLoss >= 0 ? "text-emerald-500/70" : "text-rose-500/70"}`}>
              ({summary.totalGainLossPercent.toFixed(2)}%)
            </p>
          </div>
        </div>
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 p-8 rounded-3xl shadow-sm dark:shadow-none">
          <p className="text-zinc-500 text-xs font-semibold uppercase tracking-wider mb-2">Active Positions</p>
          <p className="text-4xl font-semibold text-zinc-900 dark:text-white">{totalItems}</p>
        </div>
      </div>

      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 rounded-3xl overflow-hidden shadow-sm dark:shadow-none">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-zinc-200 dark:border-white/5 bg-zinc-50/50 dark:bg-white/5">
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400">Symbol</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400 text-right">Holdings</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400 text-right">Avg Price</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400 text-right">Current</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400 text-right">Market Value</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400 text-right">Return</th>
                <th className="px-6 py-5 text-xs font-semibold uppercase tracking-wider text-zinc-400 text-center">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-zinc-200 dark:divide-white/5">
              {loading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="animate-pulse">
                    <td colSpan={7} className="px-6 py-8"><div className="h-8 bg-zinc-100 dark:bg-zinc-800 rounded-xl w-full"></div></td>
                  </tr>
                ))
              ) : positions.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-6 py-20 text-center">
                    <p className="text-zinc-500 font-bold uppercase tracking-wider text-sm mb-4">No positions found</p>
                    <button 
                      onClick={() => setIsTradeModalOpen(true)}
                      className="text-blue-500 hover:underline font-semibold uppercase text-xs"
                    >
                      Open your first position →
                    </button>
                  </td>
                </tr>
              ) : (
                positions.map((p) => (
                  <tr key={p.symbol} className="hover:bg-zinc-50 dark:hover:bg-white/[0.02] transition-colors group">
                    <td className="px-6 py-5">
                      <Link href={`/stocks/${p.symbol.toLowerCase()}`} className="flex items-center gap-4">
                        <div className="w-10 h-10 bg-white dark:bg-zinc-800 rounded-xl flex items-center justify-center p-1.5 border border-zinc-200 dark:border-white/10 shrink-0">
                          {p.logo ? <img src={p.logo} alt="" className="max-w-full max-h-full object-contain" /> : <span className="text-xs font-bold text-zinc-500">{p.symbol}</span>}
                        </div>
                        <div>
                          <p className="font-semibold text-lg text-zinc-900 dark:text-white uppercase tracking-tight group-hover:text-blue-500 transition-colors">{p.symbol}</p>
                          <p className="text-xs font-bold text-zinc-500 uppercase tracking-wider truncate max-w-[120px]">{p.name}</p>
                        </div>
                      </Link>
                    </td>
                    <td className="px-6 py-5 text-right font-semibold text-zinc-700 dark:text-zinc-300">{p.holdingsCount.toLocaleString()}</td>
                    <td className="px-6 py-5 text-right font-semibold text-zinc-700 dark:text-zinc-300">${p.averagePrice.toFixed(2)}</td>
                    <td className="px-6 py-5 text-right font-semibold text-zinc-900 dark:text-white text-lg">${p.currentPrice.toFixed(2)}</td>
                    <td className="px-6 py-5 text-right font-semibold text-zinc-900 dark:text-white text-lg">${p.marketValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                    <td className="px-6 py-5 text-right">
                      <p className={`font-semibold text-lg ${p.totalReturn >= 0 ? "text-emerald-500" : "text-rose-500"}`}>
                        {p.totalReturn >= 0 ? "+" : ""}{p.totalReturnPercent.toFixed(2)}%
                      </p>
                      <p className={`text-xs font-bold uppercase tracking-wider ${p.totalReturn >= 0 ? "text-emerald-500/50" : "text-rose-500/50"}`}>
                        ${Math.abs(p.totalReturn).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </p>
                    </td>
                    <td className="px-6 py-5">
                      <div className="flex justify-center gap-2">
                        <button 
                          onClick={() => { setSelectedSymbol(p.symbol); setIsTradeModalOpen(true); }}
                          className="p-2.5 bg-zinc-100 dark:bg-zinc-800 rounded-xl text-zinc-500 hover:text-blue-500 hover:bg-blue-500/10 transition-all"
                          title="Trade / Adjust Position"
                        >
                          🏦
                        </button>
                        <button 
                          onClick={() => setConfirmDelete(p.symbol)}
                          className="p-2.5 bg-zinc-100 dark:bg-zinc-800 rounded-xl text-zinc-500 hover:text-rose-500 hover:bg-rose-500/10 transition-all"
                          title="Delete Position"
                        >
                          🗑️
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        {positions.length > 0 && (
          <div className="border-t border-zinc-200 dark:border-white/5">
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              onPageChange={(p) => loadPortfolio(p)}
              isLoading={loading}
            />
          </div>
        )}
      </div>
    </div>
  );
}
