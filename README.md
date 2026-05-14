![plot](assets/icon.png)


# GlossaryMcp

> GlossaryMcp gives agents one small, explicit place for project vocabulary: a plain JSONL glossary under git.

A minimal Model Context Protocol server for domain terms.

It is built for the words that slow agents down in real repositories:

- terms nobody outside the team knows
- similar words that must not be mixed up
- canonical wording for code, docs, issues, and reviews
- domain knowledge that should live with the repo instead of chat history

No vector store.
No graph model.
No database.
No semantic search.

Just a small glossary an agent can read and update without ceremony.

## Get It as a .NET Tool

[![NuGet](https://img.shields.io/nuget/v/GlossaryMcp.svg)](https://www.nuget.org/packages/GlossaryMcp/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://www.nuget.org/packages/GlossaryMcp/)

### Installation

```bash
dotnet tool install -g GlossaryMcp
```

### Update

```bash
dotnet tool update -g GlossaryMcp
```

## What It Is For

Use GlossaryMcp when a repository has vocabulary that matters:

- business terms
- product language
- process names
- abbreviations
- local naming conventions
- words with domain-specific meaning

The goal is narrow by design: make vocabulary explicit, reviewable, and easy to retrieve.

It is not:

- a memory system
- a wiki
- a knowledge graph
- a document search engine
- a replacement for README or architecture docs

GlossaryMcp stores durable vocabulary. Nothing more.

## Configuration

By default, GlossaryMcp reads and writes `glossary.jsonl` in the current working directory.

Startup options:

- `--file <path>` stores the glossary at a fixed location

Use an absolute path for `--file` when you want the glossary location to stay stable across launches.

Example MCP config:

```json
{
  "mcp": {
    "glossary": {
      "type": "local",
      "command": [
        "glossarymcp",
        "--file",
        "/absolute/path/to/glossary.jsonl"
      ]
    }
  }
}
```

If the file does not exist yet, GlossaryMcp starts with an empty glossary and creates the file on first write.

## File Format

The glossary file uses JSONL: one JSON object per line.

```jsonl
{"term":"Chargenfreigabe","description":"Fachliche Freigabe einer Charge vor Weiterverarbeitung oder Versand."}
{"term":"Sollbestand","description":"Geplanter oder erwarteter Bestand, gegen den der Istbestand verglichen wird."}
```

Each entry has two fields:

| Field | Meaning |
| --- | --- |
| `term` | The canonical domain term. |
| `description` | The full explanation the agent should use. |

The file stays intentionally strict:

- UTF-8 without BOM
- empty lines are ignored
- invalid JSON fails startup
- empty `term` fails startup
- empty `description` fails startup
- duplicate terms fail startup after normalization

Bad vocabulary should fail loudly. Silent drift costs more later.

## Tools

| Tool | Use it for |
| --- | --- |
| `find` | Search terms and descriptions with deterministic lexical ranking. |
| `add` | Append a new term when it does not already exist. |
| `edit` | Replace the full description of an existing term. |

The toolset stays intentionally small.
There is no merge command and no partial edit command.
Changes should stay explicit enough for git review.

## How It Feels in Practice

A typical agent loop looks like this:

1. A repo-specific word appears.
2. The agent calls `find` before guessing.
3. The agent uses the returned meaning for naming, design, review, or implementation.
4. If the term is missing and worth keeping, the agent calls `add`.
5. If the term exists but needs a sharper explanation, the agent calls `edit` with the full new description.

That keeps vocabulary close to the codebase and prevents repeated chat-only explanations.

## Tool Details

### `find`

Searches the full query string and its whitespace-split words against terms and descriptions.

Input:

- `query`
- `maxResults` (default `10`)

Ranking favors:

- exact term matches
- term contains matches
- exact description matches
- description contains matches
- entries that match more of the query

Example response:

```json
{
  "results": [
    {
      "entry": {
        "term": "Chargenfreigabe",
        "description": "Fachliche Freigabe einer Charge vor Weiterverarbeitung oder Versand."
      },
      "score": 1123
    }
  ]
}
```

Treat scores as ranking hints, not as stable business values.

### `add`

Appends a new glossary entry.

Input:

- `term`
- `description`

If the normalized term already exists, `add` returns the existing entry instead of writing a duplicate.

Success response:

```json
{
  "totalEntries": 12
}
```

Duplicate response:

```json
{
  "existingEntry": {
    "term": "Chargenfreigabe",
    "description": "Fachliche Freigabe einer Charge vor Weiterverarbeitung oder Versand."
  },
  "error": {
    "message": "exists already"
  }
}
```

### `edit`

Replaces the full description of an existing term.

Input:

- `term`
- `description`

The term must match an existing entry after normalization.
The original term spelling stays unchanged.

Success response:

```json
{
  "totalEntries": 12
}
```

Not found response:

```json
{
  "error": {
    "message": "term not found"
  }
}
```

## Matching and Normalization

GlossaryMcp normalizes terms for lookup and duplicate detection:

- trim
- lowercase invariant
- collapse whitespace
- replace German characters: `ä -> ae`, `ö -> oe`, `ü -> ue`, `ß -> ss`

These terms resolve to the same identity:

- `Chargenfreigabe`
- `chargenfreigabe`
- `  CHARGENFREIGABE  `

Descriptions keep their original text.

## Run Locally

From source:

```bash
cd /path/to/GlossaryMcp
dotnet run --project src/GlossaryMcp.Host -c Release -- --file ./glossary.jsonl
```

## Prompting Matters

GlossaryMcp works best when the agent knows when to use it.

A good default is:

- call `find` before guessing the meaning of a repo-specific term
- call `add` only for vocabulary that should survive future sessions
- keep descriptions short, concrete, and useful at the call site
- call `edit` when an existing description causes ambiguity
- prefer canonical project language from the glossary when naming code

Without that judgment, this is just a JSONL file.
With it, it becomes shared vocabulary.