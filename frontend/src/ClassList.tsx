import { useCallback, useEffect, useState } from "react";
import { api, ApiError, type ClassItem, type Hold } from "./api";

const REFRESH_MS = 3000;

export default function ClassList({
  studentId,
  refreshKey,
  onApplied,
}: {
  studentId: string;
  refreshKey: number;
  onApplied: (hold: Hold) => void;
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
      {error && <p className="err">{error}</p>}
      {msg && <p className="err">{msg}</p>}
      {classes.length === 0 && !error && <p>এখনো কোনো class নেই।</p>}
      {classes.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Class</th>
              <th>মোট</th>
              <th>Hold-এ</th>
              <th>নিশ্চিত</th>
              <th>খালি</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {classes.map((c) => (
              <tr key={c.id}>
                <td>{c.name}</td>
                <td>{c.totalSeats}</td>
                <td>{c.heldSeats}</td>
                <td>{c.confirmedSeats}</td>
                <td>{c.availableSeats}</td>
                <td>
                  <button
                    onClick={() => apply(c)}
                    disabled={c.availableSeats <= 0 || busyId === c.id}
                  >
                    {c.availableSeats <= 0 ? "Seat নেই" : "আবেদন করুন"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}