/**
 * How much to shrink a member's name on their card, so long names fit beside the stats. Names can be up to 50
 * characters; the card also wraps to two lines, and ends with "…" beyond that.
 */
export function memberNameScale(name: string): number {
  const length = [...name.trim()].length
  if (length <= 14) return 1
  if (length <= 22) return 0.85
  return 0.72
}
