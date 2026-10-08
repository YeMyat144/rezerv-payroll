import type { Metadata } from "next";
import Link from "next/link";
import { Providers } from "@/components/providers";
import "./globals.css";

export const metadata: Metadata = {
  title: "Rezerv Payroll",
  description: "Instructor payroll dashboard for studio businesses",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className="h-full antialiased">
      <body className="flex min-h-full flex-col bg-slate-50 text-slate-900">
        <Providers>
          <header className="border-b border-slate-200 bg-white">
            <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6">
              <Link href="/" className="flex items-center gap-2">
                <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-sm font-bold text-white">R</span>
                <span className="text-base font-semibold">Rezerv Payroll</span>
              </Link>
              <a
                href={`${process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5193"}/swagger`}
                target="_blank"
                rel="noreferrer"
                className="text-sm text-slate-500 hover:text-indigo-700"
              >
                API docs ↗
              </a>
            </div>
          </header>
          <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6 sm:px-6">{children}</main>
          <footer className="border-t border-slate-200 py-4 text-center text-xs text-slate-400">
            Rezerv engineering assessment · simplified payroll system
          </footer>
        </Providers>
      </body>
    </html>
  );
}
