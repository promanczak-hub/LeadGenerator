"use client";

import { useEffect, useState } from "react";
import Sidebar from "@/components/Sidebar";
import { supabase } from "@/lib/supabase";
import type { SourceStats } from "@/lib/types";

export default function SourcesPage() {
  const [sources, setSources] = useState<SourceStats[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      try {
        const { data, error } = await supabase.from("leads").select("source");
        if (error) throw error;

        const counts: Record<string, number> = {};
        (data || []).forEach((row: { source: string }) => {
          counts[row.source] = (counts[row.source] || 0) + 1;
        });

        const sorted = Object.entries(counts)
          .map(([source, count]) => ({ source, count }))
          .sort((a, b) => b.count - a.count);

        setSources(sorted);
      } catch (err) {
        console.error("Error loading sources:", err);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  const total = sources.reduce((sum, s) => sum + s.count, 0);

  return (
    <div className="app-layout">
      <Sidebar />
      <main className="main-content">
        <div className="page-header">
          <h1 className="page-title">Źródła danych</h1>
          <p className="page-description">
            {sources.length} aktywnych źródeł · {total.toLocaleString("pl-PL")} leadów łącznie
          </p>
        </div>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(300px, 1fr))",
            gap: 16,
          }}
        >
          {loading
            ? Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="glass-card skeleton" style={{ height: 120 }} />
              ))
            : sources.map((s) => (
                <SourceCard
                  key={s.source}
                  source={s.source}
                  count={s.count}
                  total={total}
                />
              ))}
        </div>
      </main>
    </div>
  );
}

function getSourceIcon(source: string): string {
  if (source.includes("TED")) return "🇪🇺";
  if (source.includes("Google")) return "🔍";
  if (source.includes("Pracuj")) return "💼";
  if (source.includes("Praca")) return "📋";
  if (source.includes("EZ")) return "📄";
  if (source.includes("Inwestycje")) return "🏗️";
  if (source.includes("OZE")) return "⚡";
  if (source.includes("RSS")) return "📡";
  return "📊";
}

function getSourceColor(source: string): string {
  if (source.includes("TED")) return "var(--aurora-teal)";
  if (source.includes("Google")) return "var(--aurora-amber)";
  if (source.includes("Prac")) return "var(--aurora-pink)";
  if (source.includes("EZ")) return "var(--aurora-sky)";
  if (source.includes("RSS")) return "var(--aurora-violet)";
  if (source.includes("OZE")) return "#34d399";
  return "var(--nebula-400)";
}

function SourceCard({
  source,
  count,
  total,
}: {
  source: string;
  count: number;
  total: number;
}) {
  const pct = ((count / total) * 100).toFixed(1);
  const color = getSourceColor(source);

  return (
    <div className="glass-card" style={{ position: "relative", overflow: "hidden" }}>
      <div
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          right: 0,
          height: 3,
          background: color,
          opacity: 0.8,
        }}
      />
      <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
        <span style={{ fontSize: "1.8rem" }}>{getSourceIcon(source)}</span>
        <div style={{ flex: 1 }}>
          <h3
            style={{
              fontSize: "0.95rem",
              fontWeight: 700,
              color: "var(--text-primary)",
            }}
          >
            {source}
          </h3>
          <p style={{ fontSize: "0.78rem", color: "var(--text-muted)" }}>
            {count.toLocaleString("pl-PL")} leadów · {pct}% ogółu
          </p>
        </div>
        <span
          style={{
            fontSize: "1.6rem",
            fontWeight: 800,
            color: "var(--text-primary)",
            letterSpacing: "-1px",
          }}
        >
          {count}
        </span>
      </div>
      <div
        style={{
          marginTop: 14,
          height: 6,
          background: "var(--border-glass)",
          borderRadius: 20,
          overflow: "hidden",
        }}
      >
        <div
          style={{
            width: `${pct}%`,
            height: "100%",
            background: color,
            borderRadius: 20,
            transition: "width 0.6s ease",
            minWidth: 4,
          }}
        />
      </div>
    </div>
  );
}
