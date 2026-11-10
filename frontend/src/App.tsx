import { useState } from "react";
import "./App.css";
import ClassCreate from "./ClassCreate";
import ClassList from "./ClassList";
import MyHolds from "./MyHolds";

const STORAGE_KEY = "studentId";
const VIEW_KEY = "view";

type View = "parent" | "admin";

function read(key: string): string {
  try {
    return localStorage.getItem(key) ?? "";
  } catch {
    return "";
  }
}

function write(key: string, value: string) {
  try {
    localStorage.setItem(key, value);
  } catch {
    /* storage বন্ধ থাকলেও পেজ চলবে */
  }
}

export default function App() {
  const [view, setView] = useState<View>(read(VIEW_KEY) === "admin" ? "admin" : "parent");
  const [studentId, setStudentId] = useState(() => read(STORAGE_KEY));
  const [input, setInput] = useState(studentId);
  const [error, setError] = useState("");
  const [refreshKey, setRefreshKey] = useState(0);
  const bump = () => setRefreshKey((k) => k + 1);

  function switchView(v: View) {
    setView(v);
    write(VIEW_KEY, v);
  }

  function save() {
    const value = input.trim();
    if (!/^\d{17}$/.test(value)) {
      setError("জন্ম নিবন্ধন নম্বর অবশ্যই ১৭ অঙ্কের সংখ্যা হতে হবে।");
      return;
    }
    write(STORAGE_KEY, value);
    setStudentId(value);
    setError("");
    bump();
  }

  return (
    <div className="page">
      <div className="brand">
        <div className="logo">🎓</div>
        <h1>অনলাইন ভর্তি</h1>
      </div>

      <nav className="tabs">
        <button className={view === "parent" ? "tab active" : "tab"} onClick={() => switchView("parent")}>
          অভিভাবক
        </button>
        <button className={view === "admin" ? "tab active" : "tab"} onClick={() => switchView("admin")}>
          School Admin
        </button>
      </nav>

      {view === "admin" ? (
        <main>
          <ClassCreate onCreated={bump} />
          <ClassList studentId="" refreshKey={refreshKey} onApplied={bump} readOnly />
        </main>
      ) : (
        <main>
          <section className="card">
            <div className="idbar">
              <label htmlFor="sid">শিক্ষার্থীর জন্ম নিবন্ধন নম্বর</label>
              <input
                id="sid"
                inputMode="numeric"
                maxLength={17}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                placeholder="১৭ অঙ্কের নম্বর"
              />
              <button className="btn" onClick={save}>
                সংরক্ষণ করুন
              </button>
            </div>
            {error && <div className="alert err">{error}</div>}
            {studentId && !error && <p className="hint ok">পরিচয়: {studentId}</p>}
          </section>

          {studentId ? (
            <>
              <ClassList studentId={studentId} refreshKey={refreshKey} onApplied={bump} />
              <MyHolds studentId={studentId} refreshKey={refreshKey} onChanged={bump} />
            </>
          ) : (
            <p className="hint">আবেদন করতে উপরে জন্ম নিবন্ধন নম্বর দিন।</p>
          )}
        </main>
      )}
    </div>
  );
}