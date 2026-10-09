# UI Asset Standards

When generating, importing, modifying, or querying about card assets, slots, or sprites in the Chief Complaint project, you MUST adhere to the following mathematical standards to ensure absolute uniformity across the game board.

## 1. Main Cards & Board Slots
This includes Patient Cards, Action Cards, Triage Slots, Ward Slots, and Department Slots. All must have an absolute world size of exactly **3.2 x 4.2 units**.

*   **Standard Definition (SD):**
    *   Resolution: 320 x 420 pixels
    *   Unity PPU (Pixels Per Unit): 100
*   **High Definition (HD):**
    *   Resolution: 640 x 840 pixels
    *   Unity PPU (Pixels Per Unit): 200

## 2. Symptom Mini-Cards
Symptom sub-cards must fit cleanly inside the body of the Patient Card. Their standard world size is exactly **0.9 x 1.2 units**.

*   **Standard Definition (SD):**
    *   Resolution: 90 x 120 pixels
    *   Unity PPU: 100
*   **High Definition (HD):**
    *   Resolution: 180 x 240 pixels
    *   Unity PPU: 200

## 3. Image Generation Rules
When using the `generate_image` tool to create card or slot assets:
*   Always use the `AspectRatio` parameter set to **"3:4"** (which closely matches the 320x420 / 3.2x4.2 dimension ratio).
*   Do not leave it as the default "1:1" square, as this will cause stretching or require massive PPU adjustments that break the 9-slicing math.
*   Always explicitly instruct the image generator to **fill the entire canvas/frame** and strictly forbid any white spacing, padding, margins, or background isolation. Because the image generator outputs JPEGs, any "empty" space is baked in as solid white pixels, ruining transparency.

## 4. Implementation Rules
*   **No Transform Scaling Hacks:** Never change the `Transform.localScale` of a card or slot to make it fit on the board. The scale MUST always remain strictly `(1, 1, 1)`.
*   **Control via Size & PPU:** Control the physical bounds of the sprite strictly using its pixel resolution and the `Pixels Per Unit` setting in its Texture Importer.
*   **9-Slicing:** When possible, configure the sprite with a `spriteBorder`, use `SpriteDrawMode.Sliced` on the `SpriteRenderer`, and explicitly set the `SpriteRenderer.size` property to the target world units (e.g., `3.2, 4.2`).
