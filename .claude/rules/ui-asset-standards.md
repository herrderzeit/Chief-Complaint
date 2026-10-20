---
description: Card, slot and sprite size standards for Chief Complaint board assets
paths:
  - "Assets/Sprites/**"
  - "Assets/Scripts/Board/**"
  - "Assets/Scripts/Cards/**"
  - "Assets/Scripts/UI/**"
  - "Assets/Editor/Inspectors/UICardGenerator.cs"
---

# UI Asset Standards

When generating, importing, modifying, or answering questions about card assets, slots, or sprites in Chief Complaint, follow these standards so every card and slot on the board is uniform.

## 1. Main cards and board slots
Patient Cards, Action Cards, Triage Slots, Ward Slots, and Department Slots must all have an absolute world size of exactly **3.2 x 4.2 units**.

- **Standard Definition (SD):** 320 x 420 pixels, 100 Pixels Per Unit (PPU).
- **High Definition (HD):** 640 x 840 pixels, 200 PPU.

## 2. Symptom mini-cards
Symptom sub-cards must fit inside the body of the Patient Card. Their world size is exactly **0.9 x 1.2 units**.

- **SD:** 90 x 120 pixels, 100 PPU.
- **HD:** 180 x 240 pixels, 200 PPU.

## 3. Image generation
When generating card or slot art with any image-generation tool:

- Use a **3:4** aspect ratio (it closely matches 320x420 / 3.2x4.2). Never leave the default 1:1 square, as it causes stretching or large PPU adjustments that break the 9-slicing maths.
- Explicitly instruct the generator to **fill the entire canvas**, with no white spacing, padding, margins, or isolated background. The generator outputs JPEGs, so any "empty" space is baked in as solid white and ruins transparency.

## 4. Implementation
- **No Transform scaling hacks:** never change `Transform.localScale` of a card or slot to make it fit. Scale stays `(1, 1, 1)`.
- **Control size via resolution and PPU:** set the physical bounds only through the sprite's pixel resolution and the `Pixels Per Unit` setting in its Texture Importer (use `unity command set_import_settings` while the Editor is connected).
- **9-slicing:** where possible, set a `spriteBorder`, use `SpriteDrawMode.Sliced` on the `SpriteRenderer`, and set `SpriteRenderer.size` to the target world units (for example `3.2, 4.2`).
