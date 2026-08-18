import sys
from pathlib import Path

SVG = Path("docs/architecture.svg")
PNG = Path("docs/architecture.png")

def try_cairosvg():
    try:
        import cairosvg
    except Exception:
        return False, "cairosvg not available"
    try:
        cairosvg.svg2png(url=str(SVG), write_to=str(PNG))
        return True, "converted with cairosvg"
    except Exception as e:
        return False, f"cairosvg error: {e}"

def fallback_pillow():
    try:
        from PIL import Image, ImageDraw, ImageFont
    except Exception:
        try:
            import subprocess
            subprocess.check_call([sys.executable, "-m", "pip", "install", "pillow"], stdout=subprocess.DEVNULL)
            from PIL import Image, ImageDraw, ImageFont
        except Exception as e:
            return False, f"pillow install failed: {e}"
    try:
        # Create a simple placeholder PNG
        img = Image.new("RGB", (900, 420), color=(243,244,246))
        draw = ImageDraw.Draw(img)
        try:
            font = ImageFont.truetype("arial.ttf", 20)
        except Exception:
            font = ImageFont.load_default()
        text = "Architecture diagram (placeholder)"
        # Measure text width with best-available API
        try:
            bbox = draw.textbbox((0,0), text, font=font)
            w = bbox[2] - bbox[0]
            h = bbox[3] - bbox[1]
        except Exception:
            try:
                w, h = draw.textsize(text, font=font)
            except Exception:
                try:
                    w, h = font.getsize(text)
                except Exception:
                    w, h = 400, 20
        draw.text(((900-w)/2, 30), "MT5 Source Brokers → Collectors → DB → Reconstruction → Scoring → Shadow → FIX", fill=(17,24,39), font=font)
        draw.text(((900-w)/2, 60), text, fill=(17,24,39), font=font)
        img.save(PNG)
        return True, "created placeholder with pillow"
    except Exception as e:
        return False, f"pillow error: {e}"

def main():
    if not SVG.exists():
        print("SVG not found", file=sys.stderr); sys.exit(2)
    ok, msg = try_cairosvg()
    if ok:
        print(msg); sys.exit(0)
    print("cairosvg failed or missing:", msg)
    ok2, msg2 = fallback_pillow()
    if ok2:
        print(msg2); sys.exit(0)
    print("both conversion methods failed:", msg2, file=sys.stderr); sys.exit(3)

if __name__ == '__main__':
    main()
