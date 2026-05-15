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

GlossaryMcp is not just a word list.
It is a lightweight, term-addressable project context layer for durable domain vocabulary, architecture concepts, system boundaries, data flows, and local conventions.

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
{"term":"Batch Release","description":"Formal approval of a production batch before further processing or shipping."}
{"term":"Target Stock","description":"Planned or expected inventory level used for comparison with actual stock."}
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
| `delete` | Remove a wrong or obsolete term. |

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
6. If the term is wrong or obsolete, the agent calls `delete`.

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
        "term": "Batch Release",
        "description": "Formal approval of a production batch before further processing or shipping."
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
    "term": "Batch Release",
    "description": "Formal approval of a production batch before further processing or shipping."
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

### `delete`

Removes one existing glossary entry.

Input:

- `term`

The term must match an existing entry after normalization.
Delete does not fuzzy-match and does not delete multiple entries.

Success response:

```json
{
  "totalEntries": 11,
  "deletedEntry": {
    "term": "Batch Release",
    "description": "Formal approval of a production batch before further processing or shipping."
  }
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

- `Batch Release`
- `batch release`
- `  BATCH   RELEASE  `

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

> Use the `glossary` tools before guessing repository-specific vocabulary.
>
> Call `find` when a task mentions an unfamiliar or ambiguous project-specific term, data flow, system concept, boundary, process, or convention.
> 
> Prefer canonical wording from the glossary when naming code, writing docs, creating issues, or reviewing changes.
>
> Call `add` only for durable domain-specific or architecture-specific concepts when you have a precise understanding of their meaning.
> 
> Use descriptions to capture stable meaning, responsibilities, relationships, data flow, and system context.
> 
> Call `edit` only when an existing description is wrong, ambiguous, incomplete, or outdated.
> 
> Call `delete` only when a term is wrong or obsolete.
> 
> Do not use the `glossary` as chat memory, transient notes, todos, or an unstructured wiki.

With it, it becomes shared vocabulary.