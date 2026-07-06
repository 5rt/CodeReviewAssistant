# AI Code Review Assistant

A web app I built that reviews code using an LLM. You paste in a snippet or a diff, and it comes back with a list of problems it found - security issues, bugs, style stuff - each with a severity, the line it's on, and a suggested fix.

**Live demo:** https://kind-desert-061e58f00.7.azurestaticapps.net

Heads up: the backend runs on Azure's free tier, so if nobody has used it for a while the first request takes up to a minute while the server wakes up. After that it responds in a few seconds.

<!-- ![screenshot](docs/screenshot.png) -->

## Example

Feed it something like this:

```csharp
public string GetUser(string name) {
  var query = "SELECT * FROM Users WHERE name = '" + name + "'";
  return db.Run(query);
}
```

and it flags the SQL injection as High severity with parameterized queries as the fix, plus the missing null check on `name` and the `SELECT *` as lower-severity issues. It handles other languages too - I tested it with JavaScript and it correctly picked up on `var` vs `const`/`let` instead of giving C# advice.

There's also a filter to only ask for security issues (or only bugs, or only style), and if the code is actually fine it just says no issues found rather than making something up. That last part mattered to me because LLMs love to invent problems when you ask them to find some.

## How it works

React + TypeScript frontend (hosted on Azure Static Web Apps) → ASP.NET Core API (Azure App Service) → Azure OpenAI (gpt-5-mini).

The interesting part is getting reliable output from the model. The API sends a system prompt telling the model to respond with nothing but JSON in a specific shape (severity and category limited to fixed values, line number, issue, fix). On the C# side that gets deserialized into records, so the rest of the code works with typed data instead of raw model text. The model still occasionally wraps its answer in markdown fences, so before parsing I just grab everything between the first `{` and the last `}`.

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

## Repo layout

```
Models/              request/response records (the shape the model has to return)
Services/            CodeReviewService - builds the prompt, calls the model, parses the output
Program.cs           the API endpoint and CORS setup
frontend/            the React app
.github/workflows/   deploy pipeline for the frontend
```

## Still to do

- A GitHub Action that reviews pull requests on this repo and posts the findings as PR comments
- Some unit tests around the JSON parsing
- Retry once when the model returns something unparseable
- Streaming the response into the UI

---

Tushar Das - [github.com/5rt](https://github.com/5rt)
