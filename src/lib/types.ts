export interface Lead {
  id: number;
  source: string;
  company_name: string;
  tender_title: string;
  publication_date: string | null;
  url: string | null;
  ai_score: number | null;
  ai_tags: string[] | null;
  ai_summary: string | null;
  full_content: string | null;
  inserted_at: string | null;
  updated_at: string | null;
  status: string | null;
  matched_vehicle_ids: string[] | null;
  deadline: string | null;
  contact_email: string | null;
  contact_phone: string | null;
  estimated_value: number | null;
  currency: string | null;
  cpv_code: string | null;
  industry: string | null;
  notes: string | null;
  is_favorite: boolean | null;
  notified_at: string | null;
  nip: string | null;
}

export interface CarDealership {
  id: number;
  place_id: string;
  name: string;
  address: string | null;
  postal_code: string | null;
  city: string | null;
  phone: string | null;
  email: string | null;
  website: string | null;
  rating: number | null;
  reviews_count: number | null;
  opening_hours: string | null;
  maps_url: string | null;
  business_type: string | null;
  latitude: number | null;
  longitude: number | null;
  created_at: string | null;
  updated_at: string | null;
}

export interface SourceStats {
  source: string;
  count: number;
}

export interface StatusStats {
  status: string;
  count: number;
}
