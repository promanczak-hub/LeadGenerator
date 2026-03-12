"use client";

import { useEffect, useState, useCallback } from "react";
import Sidebar from "@/components/Sidebar";
import { supabase } from "@/lib/supabase";
import type { CarDealership } from "@/lib/types";

const PAGE_SIZE = 30;

export default function DealershipsPage() {
  const [dealers, setDealers] = useState<CarDealership[]>([]);
  const [search, setSearch] = useState("");
  const [cityFilter, setCityFilter] = useState("");
  const [cities, setCities] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);

  const loadDealers = useCallback(async () => {
    setLoading(true);
    try {
      const from = (page - 1) * PAGE_SIZE;
      const to = from + PAGE_SIZE - 1;

      let query = supabase
        .from("car_dealerships")
        .select("*", { count: "exact" })
        .order("rating", { ascending: false })
        .range(from, to);

      if (search) {
        query = query.or(`name.ilike.%${search}%,address.ilike.%${search}%`);
      }
      if (cityFilter) {
        query = query.eq("city", cityFilter);
      }

      const { data, count, error } = await query;
      if (error) throw error;

      setDealers((data as CarDealership[]) || []);
      setTotal(count || 0);
    } catch (err) {
      console.error("Error loading dealers:", err);
    } finally {
      setLoading(false);
    }
  }, [page, search, cityFilter]);

  useEffect(() => {
    loadDealers();
  }, [loadDealers]);

  useEffect(() => {
    async function loadCities() {
      const { data } = await supabase
        .from("car_dealerships")
        .select("city")
        .not("city", "is", null);

      if (data) {
        const unique = [
          ...new Set(data.map((d: { city: string }) => d.city).filter(Boolean)),
        ].sort() as string[];
        setCities(unique);
      }
    }
    loadCities();
  }, []);

  const totalPages = Math.ceil(total / PAGE_SIZE);

  return (
    <div className="app-layout">
      <Sidebar />
      <main className="main-content">
        <div className="page-header">
          <h1 className="page-title">Dealerzy</h1>
          <p className="page-description">
            {total.toLocaleString("pl-PL")} salonów samochodowych
          </p>
        </div>

        {/* Filters */}
        <div className="filters-bar">
          <div className="search-wrapper">
            <span className="search-icon">🔍</span>
            <input
              type="text"
              className="search-input"
              placeholder="Szukaj po nazwie lub adresie..."
              onChange={(e) => {
                const val = e.target.value;
                clearTimeout((window as unknown as Record<string, ReturnType<typeof setTimeout>>).__dealerSearchTimeout);
                (window as unknown as Record<string, ReturnType<typeof setTimeout>>).__dealerSearchTimeout = setTimeout(() => {
                  setSearch(val);
                  setPage(1);
                }, 400);
              }}
            />
          </div>

          <select
            className="filter-select"
            onChange={(e) => {
              setCityFilter(e.target.value);
              setPage(1);
            }}
            value={cityFilter}
          >
            <option value="">Każde miasto</option>
            {cities.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>

        {/* Grid */}
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(340px, 1fr))",
            gap: 16,
          }}
        >
          {loading
            ? Array.from({ length: 6 }).map((_, i) => (
                <div
                  key={i}
                  className="glass-card skeleton"
                  style={{ height: 180 }}
                />
              ))
            : dealers.map((d) => <DealerCard key={d.id} dealer={d} />)}
        </div>

        {totalPages > 1 && (
          <div className="pagination">
            <button
              className="pagination-btn"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              ← Poprzednia
            </button>
            <span className="pagination-info">
              Strona {page} z {totalPages}
            </span>
            <button
              className="pagination-btn"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Następna →
            </button>
          </div>
        )}
      </main>
    </div>
  );
}

function DealerCard({ dealer }: { dealer: CarDealership }) {
  return (
    <div className="glass-card" style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
        <div>
          <h3
            style={{
              fontSize: "0.95rem",
              fontWeight: 700,
              color: "var(--text-primary)",
              marginBottom: 2,
            }}
          >
            {dealer.name}
          </h3>
          <p style={{ fontSize: "0.8rem", color: "var(--text-muted)" }}>
            📍 {dealer.city || "—"}{dealer.address ? `, ${dealer.address}` : ""}
          </p>
        </div>
        {dealer.rating && (
          <span
            style={{
              background: "linear-gradient(135deg, var(--aurora-amber), #f59e0b)",
              color: "white",
              padding: "3px 10px",
              borderRadius: 20,
              fontSize: "0.78rem",
              fontWeight: 700,
              whiteSpace: "nowrap",
            }}
          >
            ⭐ {dealer.rating}
          </span>
        )}
      </div>

      <div style={{ display: "flex", gap: 16, flexWrap: "wrap", fontSize: "0.82rem" }}>
        {dealer.phone && (
          <a
            href={`tel:${dealer.phone}`}
            style={{ color: "var(--nebula-500)", textDecoration: "none", fontWeight: 600 }}
          >
            📞 {dealer.phone}
          </a>
        )}
        {dealer.email && (
          <a
            href={`mailto:${dealer.email}`}
            style={{ color: "var(--nebula-500)", textDecoration: "none", fontWeight: 600 }}
          >
            ✉️ {dealer.email}
          </a>
        )}
      </div>

      <div style={{ display: "flex", gap: 8, marginTop: "auto" }}>
        {dealer.website && (
          <a
            href={dealer.website}
            target="_blank"
            rel="noopener noreferrer"
            className="pagination-btn"
            style={{ fontSize: "0.72rem", padding: "5px 10px", textDecoration: "none" }}
          >
            🌐 Strona WWW
          </a>
        )}
        {dealer.maps_url && (
          <a
            href={dealer.maps_url}
            target="_blank"
            rel="noopener noreferrer"
            className="pagination-btn"
            style={{ fontSize: "0.72rem", padding: "5px 10px", textDecoration: "none" }}
          >
            🗺️ Google Maps
          </a>
        )}
      </div>
    </div>
  );
}
