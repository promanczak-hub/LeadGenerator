import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "LeadGenerator — Intelligence Hub",
  description: "Monitoring przetargów, leadów i rynku. Twój codzienny dashboard biznesowy.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="pl">
      <body>{children}</body>
    </html>
  );
}
