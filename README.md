# GlossaryMcp

> GlossaryMcp gives coding agents one small, explicit place for project vocabulary: a plain JSONL lexicon under git.

GlossaryMcp is a minimal MCP server for domain terms.
It is built for the stuff that keeps slowing agents down in real repos:

- what a term actually means
- where two similar terms differ
- which wording is canonical
- which domain words should be written down before they turn into repeated chat context

The design goal is deliberately narrow:
**solve project vocabulary well with the smallest possible surface area.**

No vector store.
No graph model.
No database.
No semantic search.
Just a tiny lexicon an agent can read and update without ceremony.

## Why it exists

Most repo context tools try to be a whole knowledge base.
That is useful sometimes, but it also makes simple things heavier than they need to be.

A lot of day-to-day agent friction is more basic:

- a repo has domain words nobody outside the team knows
- those words matter for naming, design, code review, and bug fixing
- the meaning is stable enough to persist
- the retrieval problem is often lexical, not semantic

GlossaryMcp focuses on exactly that layer.

If an agent asks _"what does this term mean?"_ or needs to store a newly learned domain word, this repo should be enough.

## What it is

GlossaryMcp keeps all data in one file:

```text
glossary.jsonl
```

Each line is one entry:

```json
{"term":"Chargenfreigabe","description":"Fachliche Freigabe einer Charge vor Weiterverarbeitung oder Versand."}
```

Runtime model:

- one JSONL file as source of truth
- all entries loaded into RAM on startup
- no indexing step
- deterministic lexical matching
- small tool responses

That makes behavior easy to understand and easy to debug.

## When it fits

GlossaryMcp fits well when you want:

- a small repo-local lexicon for domain terms
- deterministic lookup by exact words and phrases
- a minimal write path for adding and refining definitions
- project vocabulary under normal git review instead of hidden storage

## Current tools

| Tool | Purpose |
| --- | --- |
| `find` | Search terms and descriptions with deterministic ranking. |
| `add` | Append a new term when it does not already exist. |
| `edit` | Replace the full description of an existing term. |

## Tool behavior

### `find`

Input:

- `query`
- `maxResults` (default `10`)

Behavior:

- matches the full query string
- also matches whitespace-split query tokens
- searches both `term` and `description`
- ranks key matches above description matches
- ranks exact matches above contains matches

Returns a small result list:

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

### `add`

Input:

- `term`
- `description`

Behavior:

- normalizes the term for exact identity checks
- appends a new JSONL line only when the term does not already exist
- returns the existing entry directly on exact duplicate

Success shape:

```json
{
  "totalEntries": 12
}
```

Duplicate shape:

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

Input:

- `term`
- `description`

Behavior:

- finds the existing term by normalized exact match
- replaces the full description text
- rewrites `glossary.jsonl` from the in-memory state

Success shape:

```json
{
  "totalEntries": 12
}
```

Not found shape:

```json
{
  "error": {
    "message": "term not found"
  }
}
```

## Recommended agent workflow

GlossaryMcp is meant to stay mechanically simple.
A good default loop is:

1. call `find` when a repo-specific word appears
2. continue the task using the returned meaning
3. call `add` when a genuinely new domain word shows up
4. if `add` returns `exists already`, inspect `existingEntry`
5. if the description needs refinement, call `edit` with the full new text

That keeps the write path explicit and avoids accidental hidden merges.

## Matching and normalization

Exact term identity uses normalization:

- trim
- lowercase invariant
- collapse whitespace
- German replacements: `ä -> ae`, `ö -> oe`, `ü -> ue`, `ß -> ss`

So these count as the same term:

- `Chargenfreigabe`
- `chargenfreigabe`
- `  CHARGENFREIGABE  `

## File rules

`glossary.jsonl` is intentionally strict:

- UTF-8 without BOM
- empty lines are ignored
- invalid JSON causes startup failure
- empty `term` causes startup failure
- empty `description` causes startup failure

That keeps bad data visible instead of letting it drift.

## Configuration

Default file location:

```text
./glossary.jsonl
```

Startup option:

```text
--file <path-to-glossary.jsonl>
```

If the file does not exist yet, GlossaryMcp starts with an empty in-memory store and creates the file on first write.

## Run locally

From source:

```bash
export PATH="$PATH:/home/bob/.dotnet"
cd /path/to/GlossaryMcp

dotnet run --project src/GlossaryMcp.Host -c Release -- --file ./glossary.jsonl
```

## Example MCP config

Example for a local source checkout:

```json
{
  "mcpServers": {
    "glossary": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/GlossaryMcp/src/GlossaryMcp.Host",
        "-c",
        "Release",
        "--",
        "--file",
        "/absolute/path/to/glossary.jsonl"
      ]
    }
  }
}
```