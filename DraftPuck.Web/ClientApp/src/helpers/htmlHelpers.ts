const htmlEscapes: Record<string, string> = {
  '&': '&amp;',
  '<': '&lt;',
  '>': '&gt;',
  '"': '&quot;',
  "'": '&#039;'
}

// Escape untrusted text (member names, player names, message text) before it's put into a string rendered with v-html.
export function escapeHtml(text: string): string {
  return text.replace(/[&<>"']/g, (c) => htmlEscapes[c])
}

// Escape text before using it inside a RegExp so names can't change the pattern.
export function escapeRegExp(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}
