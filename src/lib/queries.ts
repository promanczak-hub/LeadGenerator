import { supabase } from "./supabase";
import type { Lead, SourceStats, StatusStats } from "./types";

const PAGE_SIZE = 30;

export interface LeadFilters {
  search?: string;
  source?: string;
  status?: string;
  minScore?: number;
  page?: number;
}

export interface LeadsResponse {
  data: Lead[];
  count: number;
  page: number;
  totalPages: number;
}

export async function fetchLeads(
  filters: LeadFilters = {}
): Promise<LeadsResponse> {
  const { search, source, status, minScore, page = 1 } = filters;
  const from = (page - 1) * PAGE_SIZE;
  const to = from + PAGE_SIZE - 1;

  let query = supabase
    .from("leads")
    .select("*", { count: "exact" })
    .order("inserted_at", { ascending: false })
    .range(from, to);

  if (search) {
    query = query.or(
      `company_name.ilike.%${search}%,tender_title.ilike.%${search}%`
    );
  }
  if (source) {
    query = query.eq("source", source);
  }
  if (status) {
    query = query.eq("status", status);
  }
  if (minScore !== undefined) {
    query = query.gte("ai_score", minScore);
  }

  const { data, count, error } = await query;

  if (error) throw error;

  return {
    data: (data as Lead[]) || [],
    count: count || 0,
    page,
    totalPages: Math.ceil((count || 0) / PAGE_SIZE),
  };
}

export async function fetchSourceStats(): Promise<SourceStats[]> {
  const { data, error } = await supabase.rpc("get_source_stats").select();

  if (error) {
    // Fallback: manual query
    const { data: rawData, error: rawError } = await supabase
      .from("leads")
      .select("source");

    if (rawError) throw rawError;

    const counts: Record<string, number> = {};
    (rawData || []).forEach((row: { source: string }) => {
      counts[row.source] = (counts[row.source] || 0) + 1;
    });

    return Object.entries(counts)
      .map(([source, count]) => ({ source, count }))
      .sort((a, b) => b.count - a.count);
  }

  return data as SourceStats[];
}

export async function fetchStatusStats(): Promise<StatusStats[]> {
  const { data, error } = await supabase
    .from("leads")
    .select("status");

  if (error) throw error;

  const counts: Record<string, number> = {};
  (data || []).forEach((row: { status: string }) => {
    const s = row.status || "new";
    counts[s] = (counts[s] || 0) + 1;
  });

  return Object.entries(counts)
    .map(([status, count]) => ({ status, count }))
    .sort((a, b) => b.count - a.count);
}

export async function fetchDashboardStats() {
  const [
    { count: totalLeads },
    { count: newToday },
    { count: highScore },
    { count: favoriteCount },
  ] = await Promise.all([
    supabase.from("leads").select("*", { count: "exact", head: true }),
    supabase
      .from("leads")
      .select("*", { count: "exact", head: true })
      .gte("inserted_at", new Date(Date.now() - 86400000).toISOString()),
    supabase
      .from("leads")
      .select("*", { count: "exact", head: true })
      .gte("ai_score", 70),
    supabase
      .from("leads")
      .select("*", { count: "exact", head: true })
      .eq("is_favorite", true),
  ]);

  return {
    totalLeads: totalLeads || 0,
    newToday: newToday || 0,
    highScore: highScore || 0,
    favoriteCount: favoriteCount || 0,
  };
}

export async function fetchRecentLeads(limit = 10): Promise<Lead[]> {
  const { data, error } = await supabase
    .from("leads")
    .select("*")
    .order("inserted_at", { ascending: false })
    .limit(limit);

  if (error) throw error;
  return (data as Lead[]) || [];
}

export async function toggleFavorite(
  id: number,
  current: boolean
): Promise<void> {
  const { error } = await supabase
    .from("leads")
    .update({ is_favorite: !current })
    .eq("id", id);

  if (error) throw error;
}

export async function updateLeadStatus(
  id: number,
  status: string
): Promise<void> {
  const { error } = await supabase
    .from("leads")
    .update({ status, updated_at: new Date().toISOString() })
    .eq("id", id);

  if (error) throw error;
}
