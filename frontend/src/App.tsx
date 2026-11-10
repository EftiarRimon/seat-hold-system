import { useState } from "react";
import "./App.css";

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
        {studentId ? (
          <p>Class তালিকা এখানে আসবে।</p>
        ) : (
          <p>শুরু করতে উপরে জন্ম নিবন্ধন নম্বর দিন।</p>
        )}
      </main>
    </div>
  );
}