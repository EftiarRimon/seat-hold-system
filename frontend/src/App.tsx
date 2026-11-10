import { useState } from "react";
import "./App.css";
import ClassCreate from "./ClassCreate";
import ClassList from "./ClassList";
import MyHolds from "./MyHolds";

const STORAGE_KEY = "studentId";

function loadStudentId(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? "";
  } catch {
    return "";
  }
}

export default function App() {
  const [studentId, setStudentId] = useState(loadStudentId);
  const [input, setInput] = useState(studentId);
  const [error, setError] = useState("");
  const [refreshKey, setRefreshKey] = useState(0);
  const bump = () => setRefreshKey((k) => k + 1);

  function save() {
    const value = input.trim();
    if (!/^\d{17}$/.test(value)) {
      setError("জন্ম নিবন্ধন নম্বর অবশ্যই ১৭ অঙ্কের সংখ্যা হতে হবে।");
      return;
    }
    try {
      localStorage.setItem(STORAGE_KEY, value);
    } catch {
      /* storage বন্ধ থাকলেও পেজ চলবে */
    }
    setStudentId(value);
    setError("");
    bump();
  }

  return (
    <div className="page">
      <header className="topbar">
        <label htmlFor="sid">শিক্ষার্থীর জন্ম নিবন্ধন নম্বর</label>
        <input
          id="sid"
          inputMode="numeric"
          maxLength={17}
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="১৭ অঙ্কের নম্বর"
        />
        <button onClick={save}>সংরক্ষণ করুন</button>
        {studentId && !error && <span className="ok">পরিচয়: {studentId}</span>}
        {error && <span className="err">{error}</span>}
      </header>

      <main>
        <ClassCreate onCreated={bump} />

        {studentId ? (
          <>
            <ClassList studentId={studentId} refreshKey={refreshKey} onApplied={bump} />
            <MyHolds studentId={studentId} refreshKey={refreshKey} onChanged={bump} />
          </>
        ) : (
          <p>আবেদন করতে উপরে জন্ম নিবন্ধন নম্বর দিন।</p>
        )}
      </main>
    </div>
  );
}