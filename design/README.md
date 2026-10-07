# Semantic design contract

`design.graph.json` relates the UI, data, behavior and proposed implementation ownership. It is not just a visual tree. `state-machine.json` defines orthogonal states and cross-state invariants; `tokens.json` records observed measurements and separately marks proposed changes.

This is a project-specific schema, not an official Uno Platform graph format. Observed and proposed nodes carry evidence paths and confidence. The standalone package validator executes 14 structural/source-consistency checks. Passing those checks does not prove all prose semantics are consistent or that an app has been implemented. Use the acceptance brief and human review for that.
