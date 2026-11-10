import { useCallback, useEffect, useState } from "react";
import { api, ApiError, type AdminHold } from "./api";

const REFRESH_MS = 3000;

const LABEL: Record<string, string> = {
  HELD: "ফি বাকি",
  CONFIRMED: "ভর্তি নিশ্চিত",
  EXPIRED: "মেয়াদ শেষ",
  CANCELLED: "বাতিল",
};

function fmtTime(iso: string) {
  return new Date(iso.replace(/(\.\d{3})\d+/, "$1")).toLocaleString("bn-BD");
}

export default function AdminHolds({
  password,
  refreshKey,
}: {
  password: string;
  refreshKey: number;
}) {
  const [rows, setRows] = useState<AdminHold[]>([]);
  const [error, setError] = useState("");
  const [q, setQ] = useState("");

  const load = useCallback(async () => {
    try {
      setRows(await api.adminHolds(password || undefined));
      setError("");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "তালিকা আনা যায়নি।");
    }
  }, [password]);

  useEffect(() => {
    load();
    const t = setInterval(load, REFRESH_MS);
    return () => clearInterval(t);
  }, [load, refreshKey]);

  const filtered = rows.filter((r) => r.studentId.includes(q.trim()));

  return (
    <section className="card">
      <h2>আবেদনকারীদের তালিকা ({filtered.length})</h2>
      <div className="row" style={{ marginBottom: 10 }}>
        <input
          className="grow"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="জন্ম নিবন্ধন নম্বর দিয়ে খুঁজুন"
          inputMode="numeric"
        />
      </div>
      {error && <div className="alert err">{error}</div>}
      {filtered.length === 0 && !error && <p className="hint">কোনো আবেদন নেই।</p>}
      {filtered.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>জন্ম নিবন্ধন নম্বর</th>
                <th>Class</th>
                <th>অবস্থা</th>
                <th>Transaction নম্বর</th>
                <th>আবেদনের সময়</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((r) => (
                <tr key={r.holdId}>
                  <td className="mono">{r.studentId}</td>
                  <td>{r.className}</td>
                  <td>
                    <span className={`badge ${r.status}`}>{LABEL[r.status] ?? r.status}</span>
                  </td>
                  <td className="mono">{r.transactionId ?? "-"}</td>
                  <td>{fmtTime(r.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}