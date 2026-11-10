import { useState } from "react";
import { api, ApiError } from "./api";

export default function ClassCreate({
  onCreated,
  adminPassword,
}: {
  onCreated: () => void;
  adminPassword?: string;
}) {
  const [name, setName] = useState("");
  const [seats, setSeats] = useState("");
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    const n = Number(seats);
    if (!name.trim()) {
      setMsg({ ok: false, text: "Class-এর নাম লিখুন।" });
      return;
    }
    if (!Number.isInteger(n) || n <= 0) {
      setMsg({ ok: false, text: "Seat সংখ্যা ০-র বেশি পূর্ণ সংখ্যা হতে হবে।" });
      return;
    }
    setBusy(true);
    try {
      await api.createClass(name.trim(), n, adminPassword);
      setName("");
      setSeats("");
      setMsg({ ok: true, text: "নতুন class তৈরি হয়েছে।" });
      onCreated();
    } catch (err) {
      setMsg({ ok: false, text: err instanceof ApiError ? err.message : "সমস্যা হয়েছে।" });
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="card">
      <h2>নতুন class তৈরি</h2>
      <form onSubmit={submit} className="row">
        <input
          className="grow"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="যেমন: Class 6 - Morning Shift"
        />
        <input
          value={seats}
          onChange={(e) => setSeats(e.target.value)}
          placeholder="Seat সংখ্যা"
          inputMode="numeric"
          style={{ width: 130 }}
        />
        <button className="btn" disabled={busy}>
          তৈরি করুন
        </button>
      </form>
      {msg && <div className={msg.ok ? "alert ok" : "alert err"}>{msg.text}</div>}
    </section>
  );
}