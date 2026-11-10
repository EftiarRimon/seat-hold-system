import { useEffect, useState, type ReactNode } from "react";
import { api, ApiError } from "./api";

const KEY = "adminPassword";

function readPw() {
  try {
    return sessionStorage.getItem(KEY) ?? "";
  } catch {
    return "";
  }
}

export default function AdminGate({
  children,
}: {
  children: (password: string, logout: () => void) => ReactNode;
}) {
  const [required, setRequired] = useState<boolean | null>(null);
  const [password, setPassword] = useState(readPw);
  const [authed, setAuthed] = useState(false);
  const [input, setInput] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    api
      .adminStatus()
      .then(async (s) => {
        setRequired(s.required);
        if (s.required && password) {
          try {
            await api.adminLogin(password);
            setAuthed(true);
          } catch {
            setPassword("");
          }
        }
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : "সমস্যা হয়েছে।"));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function login(e: React.FormEvent) {
    e.preventDefault();
    try {
      await api.adminLogin(input);
      try {
        sessionStorage.setItem(KEY, input);
      } catch {
        /* storage বন্ধ থাকলেও চলবে */
      }
      setPassword(input);
      setAuthed(true);
      setError("");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "সমস্যা হয়েছে।");
    }
  }

  function logout() {
    try {
      sessionStorage.removeItem(KEY);
    } catch {
      /* ignore */
    }
    setPassword("");
    setInput("");
    setAuthed(false);
  }

  if (required === null) {
    return error ? <div className="alert err">{error}</div> : <p className="hint">লোড হচ্ছে...</p>;
  }

  if (!required) return <>{children("", logout)}</>;

  if (!authed) {
    return (
      <section className="card">
        <h2>Admin login</h2>
        <form onSubmit={login} className="row">
          <input
            className="grow"
            type="password"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Admin পাসওয়ার্ড"
          />
          <button className="btn">Login</button>
        </form>
        {error && <div className="alert err">{error}</div>}
      </section>
    );
  }

  return <>{children(password, logout)}</>;
}