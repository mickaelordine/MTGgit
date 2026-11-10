# MTGgit - Commander DeckBuilder (Local-First MTG EDH Deck Manager & Version Control)

A standalone C# desktop application (.NET 8) designed exclusively for building, managing, and versioning **Magic: The Gathering — Commander (EDH)** decks. 

The core differentiator of this application is a **local, Git-inspired Version History engine** with a **SourceTree-style commit graph and visual diff inspector**, combined with full offline Scryfall card search, on-demand image caching, and a dynamic UI theme that adapts to the active Commander's Color Identity.

---

## 1. Core Features & Domain Rules

1. **Commander-First Deck Creation**
   - Every deck starts by selecting a Commander (or Partner/Background pair).
   - The Commander defines the deck's **Color Identity** (`W`, `U`, `B`, `R`, `G`, or `C`).
2. **Deck Zones**
   - Cards in a deck are organized into four zones: `Commander`, `Mainboard`, `Sideboard`, and `Considering`.
3. **Color Identity Validation & Filtering**
   - **Search Filter:** When searching for cards inside a deck, the search bar automatically filters results to cards whose `ColorIdentity` is a valid subset of the Commander's `ColorIdentity`.
   - **Visual Violation Indicator (Red Dot):** If an off-color card is present in the deck (e.g., imported via text or after changing the Commander), it is flagged with `HasColorIdentityViolation = true` and displays a red warning indicator dot in the UI.
4. **Closest-Name Search Ranking**
   - The search bar queries the local SQLite database and orders results by closeness to the user's input:
     1. Exact name match
     2. Prefix match (`query%`)
     3. Substring match (`%query%`)
     4. Fuzzy / Levenshtein distance fallback
5. **Printing Switcher & Local Image Cache**
   - All cards and all historical printings/editions are stored locally in SQLite via Scryfall Bulk Data (`default_cards`).
   - Users can switch a card's printing to any released set/collector number.
   - Card images are downloaded on demand from Scryfall's CDN and cached locally on disk.
6. **Custom Card Tagging**
   - Users can attach multiple custom tags (e.g., `#Ramp`, `#Draw`, `#Removal`, `#Wincon`) to any card in a deck and group/filter the deck view by tag.
7. **Plain Text & Moxfield Import/Export**
   - Full bidirectional support for standard Plain Text (`1 Sol Ring`) and Moxfield syntax (`1 Sol Ring (CMM) 214 *F* #Ramp #Artifact`), including `SIDEBOARD:` and `CONSIDERING:` sections.
8. **Dynamic Color Identity UI Theme**
   - The application's color palette (Primary, Secondary, Accent, Header Gradients) dynamically adapts to the active Commander's Color Identity across all 32 possible MTG color combinations.
9. **Git-Like Local Version Control (SourceTree-Style)**
   - Every deck initializes with a local `main` branch.
   - **Live Working Tree Diff:** Any modification to a deck (adding/removing cards, changing quantities, moving between `Mainboard`/`Sideboard`/`Considering`, switching printings, or editing `#Tags`) computes a real-time diff against the `HEAD` commit.
   - **Commit Workflow:** When `HasUncommittedChanges == true`, the **Commit** button lights up. Committing requires a **mandatory, non-empty commit message** and saves an immutable snapshot in SQLite.
   - **SourceTree History View:** Displays the linear commit graph along the `main` branch. Users can select any historical commit to inspect:
     - The changes introduced in that specific commit (`Parent -> Commit`).
     - The cumulative diff between that commit and the current active deck (`Selected Commit -> Working Copy`).

---

## 2. Tech Stack & Architecture

- **Runtime & Language:** .NET 8 / C# 12 (Windows Desktop `.exe`)
- **Architecture Pattern:** MVVM (`CommunityToolkit.Mvvm`) + Dependency Injection (`Microsoft.Extensions.DependencyInjection`)
- **Local Database:** SQLite (`Microsoft.Data.Sqlite` + Dapper / EF Core)
- **JSON Serialization:** `System.Text.Json` (streaming deserialization for Scryfall bulk files)

### Solution Structure

```text
CommanderDeckBuilder.sln
├── src/
│   ├── CommanderDeckBuilder.Core/          # Domain models, enums, interfaces, diff engine, parsers
│   ├── CommanderDeckBuilder.Data/          # SQLite schema, repositories, Scryfall bulk ingestion & image cache
│   └── CommanderDeckBuilder.UI/            # Desktop UI (.exe), ViewModels, Views, Dynamic Theme dictionaries
└── tests/
    └── CommanderDeckBuilder.Tests/         # Unit tests for Diff Engine, Moxfield Parser, Search Ranking, CI Validation
