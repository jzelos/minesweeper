# Plan

**Task:** Start the timer on first click, not on board creation
**Status:** done

## Scope

Currently `InfoBar.StartGame()` (called from `Game.StartNewGame()`, i.e. at board
creation / restart time) immediately sets `timerRunning = true` and stamps
`timerStartTicks = SDL_GetTicks()`. `Game.Render()` calls `infoBar.UpdateTimer()`
unconditionally every frame, so the seven-segment timer starts counting the instant
the board is created/reset — before the player has made any move. This inflates
every recorded time and therefore every high score.

`Grid.HasStarted()` (returns the private `minesPlaced` flag) already exists,
already flips to `true` at the correct moment (inside `Grid.OpenCell()`, right
before `PlaceMines()` runs on the very first reveal), and is currently never
called from anywhere. It is the correct hook to key off.

Fix is confined to `InfoBar.cs` and `Game.cs`. No changes to `Grid.cs` are needed
— `HasStarted()` / `minesPlaced` semantics are already exactly right (flagging via
`MarkCell` does *not* set `minesPlaced`, so right-click flagging before the first
reveal correctly does not start the timer; chording before any reveal is a no-op
in `ChordOpen` since it requires an already-visible numbered cell, so it can't be
the trigger either).

Out of scope (explicitly not part of this change, tracked separately in
IDEAS.md): pausing the timer on window-focus-loss/menu-open, 3BV/efficiency
stats, keyboard shortcuts.

## Files affected

- `D:\Source\minesweeper\InfoBar.cs` — split "reset game state" from "actually
  start the clock". `StartGame()` no longer flips `timerRunning` or stamps
  `timerStartTicks`; a new `StartTimer()` method does that, guarded by a new
  `timerStarted` flag so it is safe to call every frame / multiple times without
  resetting the clock or reviving it after game-over.
- `D:\Source\minesweeper\Game.cs` — in `Render()`, call `infoBar.StartTimer()`
  when `grid.HasStarted()` is true, immediately before the existing
  `infoBar.UpdateTimer()` call.

## Approach

### 1. `InfoBar.cs`

Add a new private field, distinct from `timerRunning`:

```csharp
private bool timerStarted;   // true once the clock has been started for the current game
```

Change `StartGame()` to only reset state, not start the clock:

```csharp
public void StartGame(int mineCount)
{
    totalMines      = mineCount;
    flagsPlaced     = 0;
    elapsedSeconds  = 0;
    timerRunning    = false;   // was: true
    timerStarted    = false;   // new
    timerStartTicks = 0;       // was: SDL_GetTicks() — no longer meaningful until StartTimer()
    currentMode     = GameMode.Game;
}
```

Add the new method (idempotent — safe to call every frame):

```csharp
/// <summary>Starts the clock on the first actual reveal. Safe to call repeatedly;
/// only takes effect once per game, and never re-arms after StopTimer() ends a game.</summary>
public void StartTimer()
{
    if (timerStarted) return;
    timerStarted    = true;
    timerRunning    = true;
    timerStartTicks = SDL_GetTicks();
}
```

`StopTimer()`, `UpdateTimer()`, `GetElapsedSeconds()` etc. are unchanged. Note
that `StopTimer()` only clears `timerRunning`, not `timerStarted` — this is what
prevents `StartTimer()` from reviving the clock after a win/loss while
`grid.HasStarted()` remains true during the win/lose overlay.

### 2. `Game.cs`

In `Render()`, immediately before the existing timer update line, add the gate:

```csharp
int menuH = menuBar.GetHeight();
if (grid.HasStarted())
    infoBar.StartTimer();
infoBar.UpdateTimer();
infoBar.SetFlagsPlaced(grid.GetFlagCount());
...
```

No other call sites need to change. `StartNewGame()` keeps calling
`infoBar.StartGame(grid.GetTotalMines())` exactly as today — it now just means
"reset the info bar for a new game" rather than "reset and start ticking".

## Why this avoids the double-counting / off-by-one risk called out in the task

Gating *only* the call to `UpdateTimer()` on `grid.HasStarted()` (i.e. leaving
`StartGame()` as the thing that stamps `timerStartTicks`) would NOT be correct on
its own: `timerStartTicks` would still be captured at board-creation time, so the
instant `HasStarted()` first becomes true, `UpdateTimer()` would report however
many whole seconds the player spent looking at the board before clicking (e.g.
"5" instead of "0") — a visible jump/inflation, just delayed rather than removed.

The plan avoids this by moving the `SDL_GetTicks()` stamp itself into the new
`StartTimer()` method, which only runs at the moment `grid.HasStarted()` first
becomes true (checked once per frame, and `Render()` runs immediately after
`PollEvents()` in the same loop iteration as the click that caused
`minesPlaced` to flip — see `Game.Start()`'s `while (running) { PollEvents();
Render(); }` loop), so the clock's zero point is effectively the first reveal
click itself, with at most one frame (~16ms at vsync) of latency. That is the
"elapsed time should start at the first click" requirement from the task.

The `timerStarted` guard (separate from `timerRunning`) is what prevents a
second failure mode: without it, calling `StartTimer()` unconditionally every
frame once `grid.HasStarted()` is true would re-stamp `timerStartTicks` and
resurrect `timerRunning` on every frame *after* a win/loss too (since
`minesPlaced` stays `true` for a finished grid and `Render()` keeps running
while the win/lose overlay is shown) — the clock would start ticking again
behind the overlay. `timerStarted` makes `StartTimer()` a true one-shot for the
lifetime of a single game (reset only by the next `StartGame()` call), so this
cannot happen regardless of how many times or in what game mode `Render()` calls
it.

## Edge cases considered

- **First-click mine placement**: `Grid.OpenCell()` sets `minesPlaced = true`
  before checking whether the clicked cell contains a mine. Since first-click
  safety means the clicked cell and its neighbours are excluded from mine
  placement, the very first reveal can never itself be a loss — irrelevant to
  timer correctness either way, but confirms `HasStarted()` flips on exactly the
  first left-click reveal, win or lose thereafter.
- **Flagging before first reveal**: `MarkCell()` never touches `minesPlaced`, so
  right-clicking to place a flag before opening any cell correctly does **not**
  start the timer. No change needed to preserve this.
- **Chording before first reveal**: `ChordOpen()` requires `cell.IsVisible` to do
  anything, and no cell can be visible before the first `OpenCell()` call, so a
  chord (middle-click / L+R) as the very first action is a no-op and cannot be
  used to start the timer early or skip starting it. No change needed.
- **Restart mid-game (smiley click)**: `RestartGame()` -> `StartNewGame()` calls
  `grid.Initalize()` (resets `minesPlaced = false`) and `infoBar.StartGame()`
  (resets `timerStarted = false`, `timerRunning = false`, `elapsedSeconds = 0`).
  Next `Render()` frame will not call `StartTimer()` again until the player makes
  a new first reveal. Correct.
- **Win/lose overlay left open indefinitely**: covered above — `timerStarted`
  guard prevents the clock from restarting while `grid.HasStarted()` remains true
  and the overlay is showing.
- **Menu-bar / overlay open before first click**: no interaction; `HasStarted()`
  only changes on `Grid.OpenCell()`, which cannot fire while the menu/overlay is
  intercepting clicks (`Game.ProcessMouseEvent()` returns early in both cases
  before reaching grid input handling). Not in scope for this change (separate
  "pause on focus loss / menu open" idea already tracked in IDEAS.md).

## Naming decision (task question 5)

Keep `StartGame()`'s name and its role as "reset info-bar state for a new game."
Add `StartTimer()` as a new, separately-named method for "begin ticking." This is
a small API surface addition (one new public method, one new private field) with
no rename of existing public members, so no other call sites break. This was
judged clearer than overloading `StartGame()` with a `bool startTimerNow` param
or renaming `timerRunning`, since `Game.cs` needs a clean single hook it can call
unconditionally every frame from `Render()` without tracking its own "have I
already started the timer" state — that responsibility stays inside `InfoBar`.

## Testing / verification

No automated test suite exists in this repo (no `*Tests` project found). Verify
manually after the change:
1. Build (`dotnet build`) — must succeed with no new warnings in `InfoBar.cs` /
   `Game.cs`.
2. Run the game, wait several seconds on a fresh board without clicking — timer
   must stay at `000`.
3. Right-click to place a flag without revealing anything — timer must stay at
   `000`.
4. Left-click to reveal the first cell — timer must start from `000` and tick up
   normally from that moment (not jump to the seconds already elapsed while
   idle).
5. Win or lose the game — timer must stop and hold its value while the overlay
   is shown, and must **not** resume ticking behind the overlay.
6. Click the smiley to restart — timer must reset to `000` and stay there until
   the next first reveal, repeating steps 2-5 correctly.

## Risks / open questions

- None functionally significant — this is a pure timing-source change confined
  to two files, additive (one new method + one new field), with no signature
  changes to existing public methods. Risk is effectively "none," matching the
  IDEAS.md assessment.
- Open question for the user/orchestrator: should placing the *first flag*
  (right-click) before any reveal also count as "started"? Current behaviour
  (this plan preserves it) is no — only an actual reveal starts the clock. This
  matches competitive/classic Minesweeper convention (flagging doesn't cost
  time) and seems like the intended behavior per the task description ("first
  actual reveal click"), but flagging it explicitly in case the user wants
  flag-placement to also start the clock.

## Change log
- 2026-07-30 09:00 [planner] Created plan.
- 2026-07-30 [worker] Implemented as planned: added `timerStarted` field and
  `StartTimer()` method to `InfoBar.cs`, changed `StartGame()` to no longer
  start the clock, and added the `grid.HasStarted()` gate calling
  `infoBar.StartTimer()` in `Game.cs`'s `Render()` immediately before
  `infoBar.UpdateTimer()`. Could not run `dotnet build` to verify compilation
  — the `dotnet` command required interactive approval unavailable in this
  session; verified correctness via manual code review instead (see worker
  report). Status set to in-review.
- 2026-07-30 [code-reviewer] Reviewed: 0 critical, 2 warnings.
