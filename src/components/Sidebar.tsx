"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

interface NavItem {
  href: string;
  label: string;
  icon: string;
  badge?: number;
}

const NAV_ITEMS: NavItem[] = [
  { href: "/", label: "Dashboard", icon: "📊" },
  { href: "/leads", label: "Leady", icon: "🎯" },
  { href: "/sources", label: "Źródła", icon: "🔗" },
];

export default function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="sidebar">
      <div className="sidebar-logo">LeadGen</div>
      <div className="sidebar-subtitle">Nawigacja</div>
      {NAV_ITEMS.map((item) => (
        <Link
          key={item.href}
          href={item.href}
          className={`sidebar-link ${pathname === item.href ? "active" : ""}`}
        >
          <span className="icon">{item.icon}</span>
          {item.label}
          {item.badge !== undefined && (
            <span className="sidebar-badge">{item.badge}</span>
          )}
        </Link>
      ))}

      <div className="sidebar-subtitle" style={{ marginTop: "auto" }}>
        System
      </div>
      <Link
        href="/settings"
        className={`sidebar-link ${pathname === "/settings" ? "active" : ""}`}
      >
        <span className="icon">⚙️</span>
        Ustawienia
      </Link>
    </aside>
  );
}
