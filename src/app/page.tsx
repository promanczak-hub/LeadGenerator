"use client";

import { useEffect, useState } from "react";
import Sidebar from "@/components/Sidebar";
import {
  fetchDashboardStats,
  fetchRecentLeads,
  fetchSourceStats,
} from "@/lib/queries";
import type { Lead, SourceStats } from "@/lib/types";
import { formatDistanceToNow } from "date-fns";
import { pl } from "date-fns/locale";

function getSourceBadgeClass(source: string): string {
  if (source.includes("TED")) return "ted";
  if (source.includes("Google")) return "google";
  if (source.includes("Prac")) return "praca";
  if (source.includes("RSS")) return "rss";
  if (source.includes("EZ")) return "ez";
  return "";
}

function getScoreClass(score: number | null): string {
  if (!score) return "low";
  if (score >= 70) return "high";
  if (score >= 40) return "medium";
  return "low";
}

export default function DashboardPage() {
  const [stats, setStats] = useState({
    totalLeads: 0,
    newToday: 0,
    highScore: 0,
    favoriteCount: 0,
  });
  const [recentLeads, setRecentLeads] = useState<Lead[]>([]);
  const [sourceStats, setSourceStats] = useState<SourceStats[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadData() {
      try {
        const [dashStats, recent, sources] = await Promise.all([
          fetchDashboardStats(),
          fetchRecentLeads(12),
          fetchSourceStatsManual(),
        ]);
        setStats(dashStats);
        setRecentLeads(recent);
        setSourceStats(sources);
      } catch (err) {
        console.error("Dashboard load error:", err);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  return (
    <div className="app-layout">
      <Sidebar />
      <main className="main-content">
        <div className="page-header">
          <h1 className="page-title">Dashboard</h1>
          <p className="page-description">
            Przegląd leadów, przetargów i źródeł danych
          </p>
        </div>

        {/* Stats Cards */}
        <div className="stats-grid">
          <div className="stat-card purple">
            <div className="stat-label">Wszystkie Leady</div>
            <div className="stat-value">
              {loading ? "—" : stats.totalLeads.toLocaleString("pl-PL")}
            </div>
          </div>
          <div className="stat-card teal">
            <div className="stat-label">Nowe dziś</div>
            <div className="stat-value">
              {loading ? "—" : stats.newToday}
            </div>
          </div>
          <div className="stat-card pink">
            <div className="stat-label">Wysoki AI Score (70+)</div>
            <div className="stat-value">
              {loading ? "—" : stats.highScore}
            </div>
          </div>
          <div className="stat-card amber">
            <div className="stat-label">Ulubione</div>
            <div className="stat-value">
              {loading ? "—" : stats.favoriteCount}
            </div>
          </div>
        </div>

        {/* Source breakdown & Recent leads side by side */}
        <div className="charts-grid">
          {/* Sources */}
          <div className="glass-card">
            <div className="card-header">
              <h2 className="card-title">Źródła danych</h2>
              <span className="card-action">
                {sourceStats.length} źródeł
              </span>
            </div>
            {loading ? (
              <div style={{ height: 200 }} className="skeleton" />
            ) : (
              <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                {sourceStats.map((s) => (
                  <SourceBar
                    key={s.source}
                    source={s.source}
                    count={s.count}
                    max={sourceStats[0]?.count || 1}
                  />
                ))}
              </div>
            )}
          </div>

          {/* Recent */}
          <div className="glass-card">
            <div className="card-header">
              <h2 className="card-title">Ostatnie leady</h2>
              <a href="/leads" className="card-action">
                Zobacz wszystkie →
              </a>
            </div>
            {loading ? (
              <div style={{ height: 200 }} className="skeleton" />
            ) : (
              <div style={{ display: "flex", flexDirection: "column", gap: 2 }}>
                {recentLeads.map((lead) => (
                  <RecentLeadRow key={lead.id} lead={lead} />
                ))}
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

/* ---- Sub-components ---- */

function SourceBar({
  source,
  count,
  max,
}: {
  source: string;
  count: number;
  max: number;
}) {
  const pct = Math.max((count / max) * 100, 4);
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
      <span
        className={`badge badge-source ${getSourceBadgeClass(source)}`}
        style={{ minWidth: 120, justifyContent: "center" }}
      >
        {source.replace("NEWS - ", "").replace("RSS - ", "")}
      </span>
      <div
        style={{
          flex: 1,
          height: 8,
          background: "var(--border-glass)",
          borderRadius: 20,
          overflow: "hidden",
        }}
      >
        <div
          style={{
            width: `${pct}%`,
            height: "100%",
            background:
              "linear-gradient(90deg, var(--nebula-400), var(--aurora-violet))",
            borderRadius: 20,
            transition: "width 0.6s ease",
          }}
        />
      </div>
      <span
        style={{
          fontSize: "0.82rem",
          fontWeight: 700,
          color: "var(--text-secondary)",
          minWidth: 40,
          textAlign: "right",
        }}
      >
        {count}
      </span>
    </div>
  );
}

function RecentLeadRow({ lead }: { lead: Lead }) {
  const timeAgo = lead.inserted_at
    ? formatDistanceToNow(new Date(lead.inserted_at), {
        addSuffix: true,
        locale: pl,
      })
    : "";

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 12,
        padding: "10px 8px",
        borderRadius: "var(--radius-sm)",
        transition: "background var(--transition-fast)",
        cursor: "pointer",
      }}
      onMouseEnter={(e) =>
        (e.currentTarget.style.background = "var(--bg-hover)")
      }
      onMouseLeave={(e) =>
        (e.currentTarget.style.background = "transparent")
      }
    >
      <span className={`ai-score ${getScoreClass(lead.ai_score)}`}>
        {lead.ai_score ?? "—"}
      </span>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div
          style={{
            fontWeight: 600,
            fontSize: "0.85rem",
            color: "var(--text-primary)",
            overflow: "hidden",
            textOverflow: "ellipsis",
            whiteSpace: "nowrap",
          }}
        >
          {lead.company_name || "Brak nazwy"}
        </div>
        <div
          style={{
            fontSize: "0.75rem",
            color: "var(--text-muted)",
            overflow: "hidden",
            textOverflow: "ellipsis",
            whiteSpace: "nowrap",
          }}
        >
          {lead.tender_title}
        </div>
      </div>
      <span
        className={`badge badge-source ${getSourceBadgeClass(lead.source)}`}
      >
        {lead.source.replace("NEWS - ", "").replace("RSS - ", "")}
      </span>
      <span
        style={{
          fontSize: "0.72rem",
          color: "var(--text-muted)",
          whiteSpace: "nowrap",
        }}
      >
        {timeAgo}
      </span>
    </div>
  );
}

/* ---- Helper: manual source stats ---- */
async function fetchSourceStatsManual(): Promise<SourceStats[]> {
  const { supabase } = await import("@/lib/supabase");
  const { data, error } = await supabase.from("leads").select("source");
  if (error) throw error;

  const counts: Record<string, number> = {};
  (data || []).forEach((row: { source: string }) => {
    counts[row.source] = (counts[row.source] || 0) + 1;
  });

  return Object.entries(counts)
    .map(([source, count]) => ({ source, count }))
    .sort((a, b) => b.count - a.count);
}
