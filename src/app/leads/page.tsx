"use client";

import { useEffect, useState, useCallback } from "react";
import Sidebar from "@/components/Sidebar";
import { fetchLeads, toggleFavorite, updateLeadStatus } from "@/lib/queries";
import type { Lead } from "@/lib/types";
import type { LeadFilters, LeadsResponse } from "@/lib/queries";
import { formatDistanceToNow } from "date-fns";
import { pl } from "date-fns/locale";

const SOURCES = [
  "TED",
  "EZ",
  "Pracuj.pl",
  "Praca.pl",
  "NEWS - Google Search",
  "NEWS - Inwestycje/SSE",
  "OZE News",
  "RSS - Bankier - Wiadomości",
  "RSS - Money.pl",
  "RSS - 300Gospodarka",
  "RSS - Bankier - Finanse",
];

const STATUSES = ["new", "analyzed", "contacted", "won", "lost"];

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

export default function LeadsPage() {
  const [response, setResponse] = useState<LeadsResponse>({
    data: [],
    count: 0,
    page: 1,
    totalPages: 0,
  });
  const [filters, setFilters] = useState<LeadFilters>({ page: 1 });
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const loadLeads = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetchLeads(filters);
      setResponse(res);
    } catch (err) {
      console.error("Error loading leads:", err);
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    loadLeads();
  }, [loadLeads]);

  const handleSearch = (value: string) => {
    setFilters((prev) => ({ ...prev, search: value || undefined, page: 1 }));
  };

  const handleSourceFilter = (value: string) => {
    setFilters((prev) => ({
      ...prev,
      source: value || undefined,
      page: 1,
    }));
  };

  const handleStatusFilter = (value: string) => {
    setFilters((prev) => ({
      ...prev,
      status: value || undefined,
      page: 1,
    }));
  };

  const handlePageChange = (page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  };

  const handleToggleFavorite = async (lead: Lead) => {
    await toggleFavorite(lead.id, lead.is_favorite ?? false);
    loadLeads();
  };

  const handleStatusChange = async (id: number, status: string) => {
    await updateLeadStatus(id, status);
    loadLeads();
  };

  return (
    <div className="app-layout">
      <Sidebar />
      <main className="main-content">
        <div className="page-header">
          <h1 className="page-title">Leady</h1>
          <p className="page-description">
            {response.count.toLocaleString("pl-PL")} wyników
          </p>
        </div>

        {/* Filters */}
        <div className="filters-bar">
          <div className="search-wrapper">
            <span className="search-icon">🔍</span>
            <input
              type="text"
              className="search-input"
              placeholder="Szukaj po nazwie firmy lub tytule..."
              onChange={(e) => {
                const val = e.target.value;
                clearTimeout((window as unknown as Record<string, ReturnType<typeof setTimeout>>).__searchTimeout);
                (window as unknown as Record<string, ReturnType<typeof setTimeout>>).__searchTimeout = setTimeout(
                  () => handleSearch(val),
                  400
                );
              }}
            />
          </div>

          <select
            className="filter-select"
            onChange={(e) => handleSourceFilter(e.target.value)}
            value={filters.source || ""}
          >
            <option value="">Wszystkie źródła</option>
            {SOURCES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>

          <select
            className="filter-select"
            onChange={(e) => handleStatusFilter(e.target.value)}
            value={filters.status || ""}
          >
            <option value="">Każdy status</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>

        {/* Table */}
        <div className="glass-card" style={{ padding: 0, overflow: "hidden" }}>
          <table className="leads-table">
            <thead>
              <tr>
                <th style={{ width: 36 }} />
                <th>Firma</th>
                <th>Tytuł / Przetarg</th>
                <th>Źródło</th>
                <th>AI Score</th>
                <th>Status</th>
                <th>Data</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 8 }).map((_, i) => (
                  <tr key={i}>
                    {Array.from({ length: 7 }).map((__, j) => (
                      <td key={j}>
                        <div
                          className="skeleton"
                          style={{ height: 18, width: "80%" }}
                        />
                      </td>
                    ))}
                  </tr>
                ))
              ) : response.data.length === 0 ? (
                <tr>
                  <td colSpan={7}>
                    <div className="empty-state">
                      <div className="icon">🔍</div>
                      <div className="title">Brak wyników</div>
                      <p>Spróbuj zmienić filtry</p>
                    </div>
                  </td>
                </tr>
              ) : (
                response.data.map((lead) => (
                  <LeadRow
                    key={lead.id}
                    lead={lead}
                    expanded={expandedId === lead.id}
                    onToggleExpand={() =>
                      setExpandedId(
                        expandedId === lead.id ? null : lead.id
                      )
                    }
                    onToggleFavorite={() => handleToggleFavorite(lead)}
                    onStatusChange={(s) =>
                      handleStatusChange(lead.id, s)
                    }
                  />
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {response.totalPages > 1 && (
          <div className="pagination">
            <button
              className="pagination-btn"
              disabled={response.page <= 1}
              onClick={() => handlePageChange(response.page - 1)}
            >
              ← Poprzednia
            </button>
            <span className="pagination-info">
              Strona {response.page} z {response.totalPages}
            </span>
            <button
              className="pagination-btn"
              disabled={response.page >= response.totalPages}
              onClick={() => handlePageChange(response.page + 1)}
            >
              Następna →
            </button>
          </div>
        )}
      </main>
    </div>
  );
}

/* ---- Lead Row Component ---- */

function LeadRow({
  lead,
  expanded,
  onToggleExpand,
  onToggleFavorite,
  onStatusChange,
}: {
  lead: Lead;
  expanded: boolean;
  onToggleExpand: () => void;
  onToggleFavorite: () => void;
  onStatusChange: (status: string) => void;
}) {
  const timeAgo = lead.inserted_at
    ? formatDistanceToNow(new Date(lead.inserted_at), {
        addSuffix: true,
        locale: pl,
      })
    : "";

  return (
    <>
      <tr
        onClick={onToggleExpand}
        style={{ cursor: "pointer" }}
      >
        <td>
          <button
            className={`favorite-btn ${lead.is_favorite ? "active" : ""}`}
            onClick={(e) => {
              e.stopPropagation();
              onToggleFavorite();
            }}
          >
            {lead.is_favorite ? "★" : "☆"}
          </button>
        </td>
        <td className="company-cell">{lead.company_name || "—"}</td>
        <td className="title-cell">{lead.tender_title || "—"}</td>
        <td>
          <span
            className={`badge badge-source ${getSourceBadgeClass(lead.source)}`}
          >
            {lead.source.replace("NEWS - ", "").replace("RSS - ", "")}
          </span>
        </td>
        <td>
          <span className={`ai-score ${getScoreClass(lead.ai_score)}`}>
            {lead.ai_score ?? "—"}
          </span>
        </td>
        <td>
          <span
            className={`badge badge-status ${lead.status || "new"}`}
          >
            {lead.status || "new"}
          </span>
        </td>
        <td
          style={{
            fontSize: "0.78rem",
            color: "var(--text-muted)",
            whiteSpace: "nowrap",
          }}
        >
          {timeAgo}
        </td>
      </tr>
      {expanded && (
        <tr>
          <td colSpan={7} style={{ padding: 0 }}>
            <LeadDetail lead={lead} onStatusChange={onStatusChange} />
          </td>
        </tr>
      )}
    </>
  );
}

/* ---- Lead Detail Expansion ---- */

function LeadDetail({
  lead,
  onStatusChange,
}: {
  lead: Lead;
  onStatusChange: (s: string) => void;
}) {
  return (
    <div
      style={{
        padding: "20px 24px",
        background: "var(--bg-hover)",
        borderBottom: "1px solid var(--border-light)",
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: 20,
      }}
    >
      <div>
        <h4
          style={{
            fontSize: "0.82rem",
            color: "var(--text-muted)",
            textTransform: "uppercase",
            letterSpacing: "0.8px",
            marginBottom: 8,
          }}
        >
          Szczegóły
        </h4>
        {lead.ai_summary && (
          <p
            style={{
              fontSize: "0.88rem",
              color: "var(--text-secondary)",
              lineHeight: 1.6,
              marginBottom: 12,
            }}
          >
            {lead.ai_summary}
          </p>
        )}
        {lead.ai_tags && lead.ai_tags.length > 0 && (
          <div style={{ display: "flex", gap: 6, flexWrap: "wrap", marginBottom: 12 }}>
            {lead.ai_tags.map((tag, i) => (
              <span key={i} className="badge badge-source">
                {tag}
              </span>
            ))}
          </div>
        )}
        {lead.url && (
          <a
            href={lead.url}
            target="_blank"
            rel="noopener noreferrer"
            style={{
              fontSize: "0.82rem",
              color: "var(--nebula-500)",
              fontWeight: 600,
            }}
          >
            Otwórz źródło →
          </a>
        )}
      </div>
      <div>
        <h4
          style={{
            fontSize: "0.82rem",
            color: "var(--text-muted)",
            textTransform: "uppercase",
            letterSpacing: "0.8px",
            marginBottom: 8,
          }}
        >
          Kontakt & Akcje
        </h4>
        {lead.contact_email && (
          <p style={{ fontSize: "0.85rem", marginBottom: 4 }}>
            📧 {lead.contact_email}
          </p>
        )}
        {lead.contact_phone && (
          <p style={{ fontSize: "0.85rem", marginBottom: 4 }}>
            📞 {lead.contact_phone}
          </p>
        )}
        {lead.nip && (
          <p style={{ fontSize: "0.85rem", marginBottom: 4 }}>
            🏛️ NIP: {lead.nip}
          </p>
        )}
        {lead.estimated_value && (
          <p style={{ fontSize: "0.85rem", marginBottom: 12 }}>
            💰 {Number(lead.estimated_value).toLocaleString("pl-PL")}{" "}
            {lead.currency || "PLN"}
          </p>
        )}
        <div style={{ display: "flex", gap: 8, marginTop: 12 }}>
          {STATUSES.map((s) => (
            <button
              key={s}
              className={`pagination-btn ${lead.status === s ? "active" : ""}`}
              style={{ fontSize: "0.72rem", padding: "5px 10px" }}
              onClick={() => onStatusChange(s)}
            >
              {s}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
