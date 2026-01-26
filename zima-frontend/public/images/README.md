# Social Media Images

This directory contains placeholder SVG files for social media sharing. **You must convert these to PNG for production.**

## Required Images

| File | Dimensions | Purpose |
|------|------------|---------|
| `og-image.png` | 1200x630 | Facebook/LinkedIn Open Graph |
| `twitter-card.png` | 1200x600 | Twitter/X Card Image |
| `logo.png` | 200x200 | Organization logo |

## SVG Templates Provided

- `og-image.svg` - Template for OG image
- `twitter-card.svg` - Template for Twitter card
- `logo.svg` - Template for logo

## How to Convert

### Option 1: Online Tool
1. Go to https://svgtopng.com/
2. Upload the SVG file
3. Download the PNG

### Option 2: Command Line (requires Inkscape)
```bash
inkscape og-image.svg --export-png=og-image.png --export-width=1200
inkscape twitter-card.svg --export-png=twitter-card.png --export-width=1200
inkscape logo.svg --export-png=logo.png --export-width=200
```

### Option 3: Design Tool
Open in Figma, Sketch, or Adobe Illustrator and export as PNG.

## Important Notes

- Social platforms (Facebook, Twitter, LinkedIn) require PNG/JPG images
- SVG files are NOT supported for OG/Twitter meta images
- Ensure images are optimized for web (compressed)
