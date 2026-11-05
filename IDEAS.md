# Feature ideas

Scouting pass over the C# / SDL2 Minesweeper (`Program.cs`, `Game.cs`, `Grid.cs`, `InfoBar.cs`,
`Overlay.cs`, `Menu.cs`, `HighScore.cs`, `Media.cs`, `Colour.cs`). Current state: mouse-only
desktop game, menu bar with Easy/Medium/Hard + three hardcoded "Custom" presets, classic info bar
(7-segment mine counter + timer + smiley), modal win/lose/message overlay, first-click safety,
chording, question-mark markers, and a single best-time-per-difficulty JSON high score file.

Each entry is marked **[grounded]** (traceable to something in this repo) or **[speculative]**
(pattern common in other Minesweeper implementations, from prior knowledge — no web research was
performed in this pass).

## Open

- **Start the timer on first click, not on board creation** — [grounded] `InfoBar.StartGame()`
  sets `timerRunning = true` the moment `StartNewGame()` runs, so seconds accrue while the player
  is still reading the board; every recorded best time is inflated. `Grid.HasStarted()` already
  exists and is never called — it looks like the hook that was intended for exactly this. Highest
  value-per-line change in the repo because it directly affects high-score integrity.
  · effort: small · risk: none; needs `Game.Render()` to gate `infoBar.UpdateTimer()` on
  `grid.HasStarted()`
- **Keyboard support (F2 = new game, Esc = close dialog/menu, Enter = confirm)** — [grounded]
  `Game.PollEvents()` handles only QUIT, MOUSEMOTION and MOUSEBUTTON — there is no `SDL_KEYDOWN`
  case anywhere in the codebase. The overlay can currently only be dismissed by hitting a 160x34px
  button, and open submenus only close by clicking. F2-for-new-game is the single most expected
  Minesweeper shortcut. Also the first step toward any real accessibility story.
  · effort: small · risk: none
- **Fix high-score keying and wire up "Reset Best Times"** — [grounded] Three different board
  sizes (12x10/20, 24x20/80, 30x24/130) all save under the literal key `"Custom"` in
  `Game.InitializeMenuBar()`, so they overwrite each other's records; and `Options > View Best
  Times` only prints Easy/Medium/Hard, so a Custom record is unviewable. Separately,
  `HighScoreManager.ResetBest()` is fully implemented and never called from anywhere — a
  half-built flow with no menu entry. Key records as e.g. `"Custom 12x10x20"` and add a reset item.
  · effort: small · risk: existing `highscores.json` files keep their stale `"Custom"` entry
- **Store save data in %APPDATA% instead of the working directory** — [grounded]
  `HighScoreManager.FILE_PATH = "highscores.json"` is a bare relative path, so records follow the
  process working directory: launch from a shortcut or install under Program Files and scores
  silently vanish or fail to save (the `Save()` catch swallows the error). Move to
  `Environment.SpecialFolder.ApplicationData/Minesweeper/`. Prerequisite for any further
  persistence (stats, settings).
  · effort: small · risk: one-time migration of an existing local file
- **Persist last difficulty and preferences between sessions** — [grounded] `Game` hardcodes
  `currentRows = 16 / currentColumns = 16 / Medium` at field-initialiser level and always boots
  into Medium. A small `Settings` class alongside `HighScoreManager` (same JSON pattern) would let
  the game reopen on the board the player actually plays. Natural carrier for the toggles below.
  · effort: small · risk: depends on the %APPDATA% item above to be worth doing
- **Real custom-board dialog (rows / columns / mines)** — [grounded] The Options menu offers three
  fixed presets *labelled* "Custom", which reads like a placeholder for the real thing; `Grid` and
  `StartNewGame()` already accept arbitrary dimensions, so the engine side is done. Blocker is
  input: there is no `SDL_TEXTINPUT` handling and `GameOverlay` renders static string arrays only,
  so this needs either +/- stepper buttons (cheaper, fits the existing click-only overlay) or a
  real text field. Also wants a density sanity check — `Grid.Initalize` will happily accept more
  mines than cells and `PlaceMines` would spin forever.
  · effort: medium · risk: overlay currently has no input widgets; guard against mines >= cells
- **Game statistics (played / won / win %, current + best streak, average time)** — [grounded]
  `HighScoreManager` already proves the persistence pattern, `TriggerWin`/`TriggerGameOver` are
  clean single choke points to record outcomes, and `GameOverlay.ShowMessage(title, string[])`
  can render a stats panel with zero new UI work. Best time alone is thin compared to what players
  expect from a game that already tracks records.
  · effort: medium · risk: none; sits on top of the %APPDATA% change
- **Sound effects (open / flag / explosion / win)** — [grounded] SDL2_mixer and its codec DLLs
  already ship into `bin/Debug/net8.0/` via the `Sayers.SDL2.Core` package, so the dependency is
  paid for and unused; `Media` is the obvious home next to `LoadTexures`/`LoadFonts`. Include a
  mute toggle in Options. Big perceived-polish win for the current UX-polish push.
  · effort: medium · risk: needs `SDL_INIT_AUDIO` (currently only `SDL_INIT_VIDEO` is initialised)
  and asset sourcing/licensing for the sounds
- **Pressed / "surprised" smiley and cell press feedback** — [grounded] `Media.DrawSmileyFace()`
  has exactly three states (normal, sunglasses win, X-eyes lose); the classic game also shows an
  "O" face while the mouse is held down, and depresses the cell under the cursor. `Grid` already
  tracks `hoveredCellX/Y` and `Game` already tracks `leftButtonDown`/`rightButtonDown` for
  chording, so the state needed is all present. Fits the recent button-state/polish commits.
  · effort: small · risk: none
- **Toggle for question-mark markers** — [grounded] `Grid.MarkCell()` hardcodes the
  None -> Flag -> Question -> None cycle with no way out; every other implementation makes "?"
  optional because experienced players find the extra right-click a misclick trap.
  · effort: small · risk: none; pairs with the settings item
- **Pause the timer when the window loses focus or a menu is open** — [grounded] No
  `SDL_WINDOWEVENT` is handled at all, and `InfoBar.UpdateTimer()` keeps counting wall-clock while
  a submenu is open or the app is alt-tabbed away — another way recorded times drift from reality.
  · effort: small · risk: must not become a pause-abuse loophole; only pause on focus loss
- **Preserve square cells / letterbox the grid on resize** — [grounded] The window is created with
  `SDL_WINDOW_RESIZABLE` and `Grid.Draw()` derives `cellWidth`/`cellHeight` independently from the
  window rect, so dragging a corner produces visibly rectangular cells and off-centre flags; the
  mine sprite has a matching `// TODO preserve aspect ratio of source image` in `Media.Draw()`.
  Centre a uniform-cell grid inside the available area instead.
  · effort: small · risk: interacts with `ResizeWindowToFit()`'s min-size logic
- **Zoom / cell-size preference** — [grounded] `Resources.CELL_SIZE` is a hard `const 32`, which
  makes a 30x16 Hard board ~1000px wide with no way to shrink it on a small laptop screen or
  enlarge it for low-vision players. Exposing 24/32/40 in Options is a two-line change plus a
  window re-fit, and is a genuine accessibility feature, not just cosmetics.
  · effort: small · risk: depends on the aspect-ratio fix to look right
- **Selectable colour themes incl. high-contrast and colourblind-safe numbers** — [grounded] The
  last three commits are literally colour-contrast and dialog-legibility work, and `Colour.cs`
  centralises the whole palette — but as `public static` fields, so themes need them to become
  instance/lookup-based first. The classic 1-8 palette puts Number1 (blue) next to Number4 (dark
  blue) and Number3 (red) next to Number5 (dark red), which is exactly the pair confusion
  deuteranopes report.
  · effort: medium · risk: mechanical refactor of every `Colour.X` reference site
- **Seeded boards ("replay this board" / share a seed)** — [grounded] `Grid.PlaceMines()` news up
  an unseeded `Random` per game, so a board can never be replayed. Threading a seed through
  `Initalize` and surfacing it in the lose dialog ("Retry this board") is cheap and gives losing
  players an immediate reason to click again.
  · effort: small · risk: seed must be captured before first click since mine placement is deferred
- **No-guess (guaranteed solvable) board generation** — [speculative, competitor pattern] The
  headline feature of modern implementations (minesweeper.online, Simon Tatham's Mines): generate,
  run a constraint solver, regenerate until the board is deducible from the first click. This
  codebase's current generator is pure rejection sampling, so 50/50 guesses at the end of Hard
  boards are common and are the main reason players abandon a run. High player value, but the
  largest item here — needs a real solver plus a generation timeout for dense custom boards.
  · effort: large · risk: generation latency on 30x24/130 boards; needs its own difficulty flag so
  no-guess times don't pollute the classic leaderboards
- **3BV, 3BV/s and efficiency on the win screen** — [speculative, enthusiast-community standard]
  Computable from the final board with a small flood-fill pass; `TriggerWin()` already has the
  elapsed time and the full grid in hand, and the win overlay has room. Appeals to the speedrun
  audience that a best-times feature implicitly courts.
  · effort: medium · risk: only meaningful once the timer bug above is fixed
- **Left-click-only "flag mode" toggle** — [speculative, mobile/trackpad convention] `Game`
  already routes button 1 vs button 3 in one place, so a mode flag in `InfoBar` (next to the
  smiley) is contained. Serves trackpad and single-button-mouse users who currently cannot flag
  comfortably; also the accessible path for anyone who can't chord.
  · effort: small · risk: none
- **README / build docs** — [grounded] The repo contains no `.md` files whatsoever (only
  `LICENSE`), yet it has a non-obvious native dependency chain (SDL2 + image + ttf DLLs, the
  `media/` content copy rule, and `Media.LoadFontAtlas` hardcoding `media\roboto-regular.ttf`
  while an unused `swansea.ttf` also ships). A short README would prevent the next contributor —
  or future you — from rediscovering all of that.
  · effort: small · risk: none
- **Dead-code cleanup pass** — [grounded] `Grid.HasStarted()`, `HighScoreManager.ResetBest()`,
  `MenuBar.CloseMenus()` and the whole `Mouse` class (`Game` tracks its own
  `leftButtonDown`/`rightButtonDown` instead of reading `Mouse.ButtonState`, and
  `UpdateCordinates` is commented out) are all unreferenced. Several are the loose ends of the
  ideas above rather than junk — worth resolving deliberately, not just deleting.
  · effort: small · risk: none

## Done

_(nothing yet — no PLAN.md present at the time of this pass)_

## Change log
- 2026-07-30 [feature-scout] Created IDEAS.md. Added 20 ideas / updated 0 / marked 0 done.
