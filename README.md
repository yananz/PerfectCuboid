# PerfectCuboid

Search for a perfect cuboid: three integer edges for which all three face
diagonals and the space diagonal are integers.

No perfect cuboid is currently known. This program provides two complementary
Euler-brick searches:

- `EulerBrick` uses the fast Pythagorean-triple construction in
  `EulerBrick.cs`. It searches a useful family of Euler bricks, but it is not a
  proof-complete enumeration of every Euler brick.
- `CompleteEuler` generates Pythagorean edge pairs and treats them as a graph.
  A triangle in that graph is an Euler brick, so this mode finds every
  primitive Euler brick whose three edges are within the requested inclusive
  range.

Both searches normalize scaled copies and apply necessary divisibility rules
and quadratic-residue sieves before performing an exact large-integer square
root.

## Usage

### Ready-to-run Windows 11 build

Download the self-contained 64-bit Windows build from the repository's GitHub
Releases page. Keep both `PerfectCuboid.exe` and `PerfectCuboid.dll.config` in
the same directory, then run it from PowerShell or Command Prompt; no separate
.NET installation is required:

```text
.\PerfectCuboid.exe 1 240 CompleteEuler
```

The executable is not code-signed, so Windows SmartScreen may ask you to
confirm that you trust it.

```text
PerfectCuboid.exe <from> <to> <action>
```

Examples:

```text
# Complete search for primitive Euler bricks with every edge in [1, 100000]
PerfectCuboid.exe 1 100000 CompleteEuler

# Search the formula family for Euclid parameters m in (1, 100000]
PerfectCuboid.exe 1 100000 EulerBrick

# Run arithmetic, filter, known-Euler-brick, and complete-search regressions
PerfectCuboid.exe 1 2 Testing
```

The lower limit is supplied at runtime. A verified published lower bound can
therefore be used in a future search without hardcoding it into the program.

`CompleteEuler` stores its Pythagorean graph in memory. Use bounded intervals
and increase them gradually while monitoring memory consumption.

## Long-running and distributed searches

`CompleteEuler` supports resumable checkpoints, persistent result logs, memory
limits, and disjoint shards based on the primitive brick's smallest edge:

```text
PerfectCuboid.exe 1 1000000 CompleteEuler \
  --smallest-from 1 \
  --smallest-to 250000 \
  --checkpoint search-1.checkpoint \
  --results discoveries.log \
  --max-memory-mb 8192 \
  --checkpoint-every 1000 \
  --progress-seconds 10
```

To distribute the same global edge range, give each process or machine a
different non-overlapping `--smallest-from`/`--smallest-to` interval. Every
shard must use the same global `<from>` and `<to>` values because the other two
edges may lie outside the shard's smallest-edge interval.

The graph is rebuilt when a process restarts, but completed smallest-edge
partitions are skipped using the checkpoint. Perfect-cuboid discoveries and
run summaries are appended to the results file. Use a separate checkpoint file
per shard; result files may be collected together after the jobs finish.

`--no-resume` replaces the checkpoint for a fresh run. Existing discoveries in
the results log are retained and de-duplicated.
