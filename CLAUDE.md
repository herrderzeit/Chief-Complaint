# Unity project
- Before editing any scene, prefab, or asset, run `unity status`. If an Editor is connected (state "ready"), drive it with `unity command <name>` instead of hand-editing .unity/.prefab/.asset YAML.
- Run `unity command` to see which commands exist; don't assume names.
- If status won't connect, run `unity pipeline list` to check for Safe Mode (compile errors), then fix the C# and restart Unity.
- Only edit scene/asset files directly when no Editor is reachable, and say so.
