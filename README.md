# AI Code Review Assistant

A web app I built that reviews code using an LLM. You paste in a snippet or a diff, and it comes back with a list of problems it found - security issues, bugs, style stuff - each with a severity, the line it's on, and a suggested fix.

**Live demo:** https://kind-desert-061e58f00.7.azurestaticapps.net

Heads up: the backend runs on Azure's free tier, so if nobody has used it for a while the first request takes up to a minute while the server wakes up. After that it responds in a few seconds.

<table>
  <tr>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/df50c371-6db7-4204-9dfd-8bc86fb587a0" alt="Review results in the web app" width="100%" />
      <br /><sub>Review results in the app</sub>
    </td>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/2832ee83-07cb-4401-85bc-f0ec909071e6" alt="Automated review comment on a pull request" width="100%" />
      <br /><sub>The tool reviewing a pull request on this repo</sub>
    </td>
  </tr>
</table>

## Example

Feed it something like this:

```csharp
public string GetUser(string name) {
  var query = "SELECT * FROM Users WHERE name = '" + name + "'";
  return db.Run(query);
}
```

and it flags the SQL injection as High severity with parameterized queries as the fix, plus the missing null check on `name` and the `SELECT *` as lower-severity issues. It handles other languages too - I tested it with JavaScript and it correctly picked up on `var` vs `const`/`let` instead of giving C# advice.

There's also a filter to only show security issues (or only bugs, or only style), and if the code is actually fine it just says no issues found rather than making something up. That last part mattered to me because LLMs love to invent problems when you ask them to find some.

## It reviews its own pull requests - and blocks bad ones

This repo has a GitHub Action that runs on every pull request: it takes the PR's diff, sends it to the deployed API, and posts the findings back as a comment. If it finds anything High-severity, the check fails - and because `main` has a branch rule requiring that check to pass, the PR literally can't be merged. So the repo won't let vulnerable code in. I tested this by opening a PR with a SQL-injection sample and watching the merge button lock.

There's also a second action (Backend CI) that builds the API and runs the unit tests on every PR, so the same PR gets checked by a compiler, a test suite, and the AI reviewer.

## How it works

React + TypeScript frontend (hosted on Azure Static Web Apps) → ASP.NET Core API (Azure App Service) → Azure OpenAI (gpt-5-mini).

The interesting part is getting reliable output from the model. The API sends a system prompt telling the model to respond with nothing but JSON in a specific shape (severity and category limited to fixed values, line number, issue, fix). On the C# side that gets deserialized into records, so the rest of the code works with typed data instead of raw model text. The model still occasionally wraps its answer in markdown fences, so before parsing I just grab everything between the first `{` and the last `}`. And if a reply is genuinely broken, the service hands the model its own bad output and asks it to fix it, once.

The parsing lives in its own class (`ReviewParser`) separate from the Azure client, which is what makes it unit-testable - there are five xUnit tests covering the happy path, the markdown-fence case, garbage input, case-insensitive matching, and clean code.

Since the API is public and spends my Azure credit, it's rate-limited (5 requests a minute, returns 429 after that) and rejects anything over 100KB before it ever calls the model.

The other thing I put effort into is auth. There are no API keys anywhere - not in the code, not in the repo, not in the CI pipeline. Locally the app authenticates as me through `az login`; in production the App Service has a managed identity with one role (Cognitive Services OpenAI User) on the OpenAI resource. Same code path both ways via `DefaultAzureCredential`.

The frontend gets deployed automatically by GitHub Actions on every push to main. The API URL gets baked into the frontend build through a `VITE_API_URL` repo variable.

## Running it locally

You need the .NET 10 SDK, Node.js, the Azure CLI, and an Azure OpenAI resource with a chat model deployed.

Backend:

```bash
cd CodeReviewAssistant
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Deployment" "YOUR-DEPLOYMENT-NAME"
dotnet run
```

Runs on http://localhost:5139. Your `az login` account needs the Cognitive Services OpenAI User role on the OpenAI resource, otherwise you'll get permission errors.

Frontend, in a second terminal:

```bash
cd frontend
npm install
npm run dev
```

Opens on http://localhost:5173 and talks to localhost:5139 by default.

Run the tests:

```bash
dotnet test tests/CodeReviewAssistant.Tests
```

## Repo layout

```
Models/              request/response records (the shape the model has to return)
Services/            CodeReviewService (calls the model) + ReviewParser (parses the output)
Program.cs           the API endpoint, CORS, and rate limiting
frontend/            the React app
tests/               xUnit tests for the parser
.github/workflows/   frontend deploy, backend CI, and the PR review + quality gate
```

## A design note

The quality gate blocks on any High-severity finding, which is right for a demo. On a real team you'd worry about false positives blocking legit work, so you'd probably gate only on High *security* findings, or make it a warning with a manual override. Same with the rate limiter - it's global right now (fine when the thing being protected is my own credit), but you'd partition it per-user for a multi-tenant app.

## Still to do

- Inline PR comments attached to specific lines, instead of one summary comment
- Structured Outputs (schema-enforced JSON) instead of prompting for the shape
- Streaming the response into the UI

---

Tushar Das - [github.com/5rt](https://github.com/5rt)

Longer write-up with the full build and everything that went wrong: [docs/PROJECT_REPORT.md](docs/PROJECT_REPORT.md)
