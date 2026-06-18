f=../dist/ifdian-sponsor.svg
W=$(awk -F'"' '/viewBox=/ {split($2, a, " "); print a[3]}' "$f")
H=$(awk -F'"' '/viewBox=/ {split($2, a, " "); print a[4]}' "$f")
RW=$((W-2)); RH=$((H-2))
RECT="<rect x=\"1\" y=\"1\" width=\"$RW\" height=\"$RH\" fill=\"none\" stroke=\"#ddd\" stroke-width=\"1\"/>"
awk -v rect="$RECT" '
/<\/svg>/ && !inserted {
    print "  " rect
    print
    inserted = 1
    next
}
{print}
' "$f" > "$f.tmp" && mv "$f.tmp" "$f"