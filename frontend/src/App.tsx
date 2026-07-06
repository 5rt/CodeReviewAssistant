import { useState } from "react";

interface ReviewComment {
  severity: string;
  category: string;
  line: number | null;
  issue: string;
  suggestedFix: string;
}

const FOCUSES = [
  { key: "everything", label: "All issues" },
  { key: "security", label: "Security" },
  { key: "bug", label: "Bugs" },
  { key: "style", label: "Style" },
];

const EXAMPLE = `public string GetUser(string name) {
  var query = "SELECT * FROM Users WHERE name = '" + name + "'";
  return db.Run(query);
}`;

const API = import.meta.env.VITE_API_URL ?? "http://localhost:5139";

const sevClass = (s: string) =>
  s.toLowerCase() === "high" ? "high" : s.toLowerCase() === "medium" ? "med" : "low";

export default function App() {
  const [code, setCode] = useState("");
  const [focus, setFocus] = useState("everything");
  const [issues, setIssues] = useState<ReviewComment[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function review() {
    setLoading(true);
    setError("");
    setIssues(null);
    try {
      const res = await fetch(`${API}/api/review`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ diff: code, focus: "everything" }),
      });
      if (!res.ok) throw new Error(`The review service returned ${res.status}. Check the API is running.`);
      setIssues(await res.json());
    } catch (e) {
      setError(e instanceof Error ? e.message : "The review couldn't be completed. Try again.");
    } finally {
      setLoading(false);
    }
  }

  // Tabs filter the results on screen, instantly
  const visible =
    issues === null
      ? null
      : focus === "everything"
      ? issues
      : issues.filter((i) => i.category.toLowerCase() === focus);

  const count = (sev: string) =>
    visible?.filter((i) => i.severity.toLowerCase() === sev).length ?? 0;

  return (
    <div className="wrap">
      <header className="header">
        <div className="logo">
          <div className="logo-mark">{"</>"}</div>
          <div>
            <h1>Code Review Assistant</h1>
            <p>AI-augmented static review</p>
          </div>
        </div>
        <span className="badge">ASP.NET Core · Azure OpenAI</span>
      </header>

      <section className="hero">
        <h2>
          Paste code. Get a <em>senior review</em> in seconds.
        </h2>
        <p>
          Flags security vulnerabilities, bugs, and style issues with suggested
          fixes, powered by a structured-output LLM pipeline on Azure.
        </p>
      </section>

      <div className="editor">
        <div className="editor-bar">
          <span className="dot" style={{ background: "#ff5f57" }} />
          <span className="dot" style={{ background: "#febc2e" }} />
          <span className="dot" style={{ background: "#28c840" }} />
          <span className="name">snippet.cs</span>
          <button className="example-btn" onClick={() => setCode(EXAMPLE)}>
            Try an example
          </button>
        </div>
        <textarea
          value={code}
          onChange={(e) => setCode(e.target.value)}
          placeholder="Paste a code snippet or unified diff…"
          spellCheck={false}
        />
      </div>

      <div className="controls">
        <div className="segmented">
          {FOCUSES.map((f) => (
            <button
              key={f.key}
              className={focus === f.key ? "active" : ""}
              onClick={() => setFocus(f.key)}
            >
              {f.label}
            </button>
          ))}
        </div>
        <button className="review-btn" onClick={review} disabled={loading || !code.trim()}>
          {loading ? "Reviewing…" : "Review code"}
        </button>
      </div>

      {error && <div className="error">{error}</div>}

      {loading && (
        <div className="loading">
          <div className="spinner" />
          Analysing your code with gpt-5-mini…
        </div>
      )}

      {visible && visible.length > 0 && (
        <>
          <div className="summary">
            <span className="pill high"><b>{count("high")}</b> high</span>
            <span className="pill med"><b>{count("medium")}</b> medium</span>
            <span className="pill low"><b>{count("low")}</b> low</span>
          </div>
          {visible.map((i, idx) => (
            <article className={`card ${sevClass(i.severity)}`} key={idx}>
              <div className="card-top">
                <span className="sev">{i.severity}</span>
                <span className="cat">{i.category}</span>
                {i.line != null && <span className="line-chip">line {i.line}</span>}
              </div>
              <p className="issue">{i.issue}</p>
              <div className="fix">
                <b>Suggested fix</b> — {i.suggestedFix}
              </div>
            </article>
          ))}
        </>
      )}

      {visible && visible.length === 0 && issues && issues.length > 0 && (
        <div className="empty">No {FOCUSES.find((f) => f.key === focus)?.label.toLowerCase()} found in this review.</div>
      )}

      {issues && issues.length === 0 && (
        <div className="empty">No issues found — this snippet looks clean. 🎉</div>
      )}

      <footer className="footer">
        <span>Built by Tushar Das</span>
        <span>React · TypeScript · ASP.NET Core · Azure OpenAI · GitHub Actions</span>
      </footer>
    </div>
  );
}