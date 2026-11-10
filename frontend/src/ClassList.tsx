import { useCallback, useEffect, useState } from "react";
import { api, ApiError, type ClassItem, type Hold } from "./api";

const REFRESH_MS = 3000;

export default function ClassList({
  studentId,
  refreshKey,
  onApplied,
  readOnly = false,
}: {
  studentId: string;
  refreshKey: number;
  onApplied: (hold: Hold) => void;
  readOnly?: boolean;
}) {
  const [classes, setClasses] = useState<ClassItem[]>([]);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");
  const [busyId, setBusyId] = useState("");

  const load = useCallback(async () => {
    try {
      setClasses(await api.listClasses());
      setError("");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "তালিকা আনা যায়নি।");
    }
  }, []);

  useEffect(() => {
    load();
    const t = setInterval(load, REFRESH_MS);
    return () => clearInterval(t);
  }, [load, refreshKey]);

  async function apply(c: ClassItem) {
    setBusyId(c.id);
    setMsg("");
    try {
      const hold = await api.createHold(c.id, studentId);
      onApplied(hold);
      load();
    } catch (err) {
      setMsg(err instanceof ApiError ? err.message : "আবেদন করা যায়নি।");
      load();
    } finally {
      setBusyId("");
    }
  }

  return (
    <section className="card">
      <h2>Class তালিকা</h2>
      {error && <div className="alert err">{error}</div>}
      {msg && <div className="alert err">{msg}</div>}
      {classes.length === 0 && !error && <p className="hint">এখনো কোনো class নেই।</p>}

      <div className="grid">
        {classes.map((c) => {
          const confirmedPct = (c.confirmedSeats / c.totalSeats) * 100;
          const heldPct = (c.heldSeats / c.totalSeats) * 100;
          const full = c.availableSeats <= 0;
          return (
            <div className="class-card" key={c.id}>
              <h3>{c.name}</h3>
              <div className="stats">
                <span className="chip">মোট {c.totalSeats}</span>
                <span className="chip">Hold-এ {c.heldSeats}</span>
                <span className="chip">নিশ্চিত {c.confirmedSeats}</span>
                <span className={full ? "chip full" : "chip free"}>
                  {full ? "Seat নেই" : `খালি ${c.availableSeats}`}
                </span>
              </div>
              <div className="bar">
                <div className="confirmed" style={{ width: `${confirmedPct}%` }} />
                <div className="held" style={{ width: `${heldPct}%` }} />
              </div>
              <div className="legend">
                <span><i className="dot" style={{ background: "var(--ok)" }} />নিশ্চিত</span>
                <span><i className="dot" style={{ background: "#f59e0b" }} />Hold-এ</span>
              </div>
              {!readOnly && (
                <button className="btn" onClick={() => apply(c)} disabled={full || busyId === c.id}>
                  {full ? "Seat নেই" : "আবেদন করুন"}
                </button>
              )}
            </div>
          );
        })}
      </div>
    </section>
  );
}