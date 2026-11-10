import { useCallback, useEffect, useState } from "react";
import { api, ApiError, type Hold } from "./api";

const REFRESH_MS = 3000;
const TXN_RE = /^[A-Za-z0-9]{6,30}$/;

const LABEL: Record<string, string> = {
  HELD: "ফি বাকি",
  CONFIRMED: "ভর্তি নিশ্চিত",
  EXPIRED: "মেয়াদ শেষ",
  CANCELLED: "বাতিল",
};

function toMs(iso: string) {
  return new Date(iso.replace(/(\.\d{3})\d+/, "$1")).getTime();
}

function fmt(ms: number) {
  const s = Math.max(0, Math.floor(ms / 1000));
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`;
}

export default function MyHolds({
  studentId,
  refreshKey,
  onChanged,
}: {
  studentId: string;
  refreshKey: number;
  onChanged: () => void;
}) {
  const [holds, setHolds] = useState<Hold[]>([]);
  const [now, setNow] = useState(Date.now());
  const [error, setError] = useState("");
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busyId, setBusyId] = useState("");
  const [txn, setTxn] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    try {
      setHolds(await api.myHolds(studentId));
      setError("");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "আবেদন তালিকা আনা যায়নি।");
    }
  }, [studentId]);

  useEffect(() => {
    load();
    const t = setInterval(load, REFRESH_MS);
    return () => clearInterval(t);
  }, [load, refreshKey]);

  useEffect(() => {
    const t = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(t);
  }, []);

  async function act(h: Hold, kind: "pay" | "cancel") {
    setBusyId(h.holdId);
    setMsg(null);
    try {
      if (kind === "pay") {
        await api.confirmHold(h.holdId, studentId, (txn[h.holdId] ?? "").trim());
        setMsg({ ok: true, text: "ভর্তি ফি জমা হয়েছে, ভর্তি নিশ্চিত।" });
      } else {
        await api.cancelHold(h.holdId, studentId);
        setMsg({ ok: true, text: "আবেদন বাতিল হয়েছে।" });
      }
    } catch (err) {
      setMsg({ ok: false, text: err instanceof ApiError ? err.message : "সমস্যা হয়েছে।" });
    } finally {
      setBusyId("");
      await load();
      onChanged();
    }
  }

  return (
    <section className="card">
      <h2>আমার আবেদন</h2>
      {error && <div className="alert err">{error}</div>}
      {msg && <div className={msg.ok ? "alert ok" : "alert err"}>{msg.text}</div>}
      {holds.length === 0 && !error && <p className="hint">এখনো কোনো আবেদন নেই।</p>}

      <div className="hold-list">
        {holds.map((h) => {
          const left = toMs(h.expiresAt) - now;
          const status = h.status === "HELD" && left <= 0 ? "EXPIRED" : h.status;
          const active = status === "HELD";
          const value = txn[h.holdId] ?? "";
          const validTxn = TXN_RE.test(value.trim());
          return (
            <div className="hold" key={h.holdId}>
              <div>
                <div className="name">{h.className ?? h.classId}</div>
                <span className={`badge ${status}`}>{LABEL[status] ?? status}</span>{" "}
                {active && <span className="timer">⏱ {fmt(left)}</span>}
                {status === "CONFIRMED" && h.transactionId && (
                  <div className="hint">
                    Transaction নম্বর: <span className="mono">{h.transactionId}</span>
                  </div>
                )}
              </div>
              {status !== "CONFIRMED" && status !== "CANCELLED" && (
                <div className="pay">
                  <input
                    value={value}
                    disabled={!active}
                    onChange={(e) => setTxn({ ...txn, [h.holdId]: e.target.value })}
                    placeholder="Transaction নম্বর (৬–৩০ অক্ষর/সংখ্যা)"
                  />
                  <button
                    className="btn"
                    disabled={!active || !validTxn || busyId === h.holdId}
                    onClick={() => act(h, "pay")}
                  >
                    ভর্তি ফি দিন
                  </button>
                  <button
                    className="btn ghost"
                    disabled={!active || busyId === h.holdId}
                    onClick={() => act(h, "cancel")}
                  >
                    বাতিল
                  </button>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </section>
  );
}