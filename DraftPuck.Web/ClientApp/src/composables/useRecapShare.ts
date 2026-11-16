import { ref } from 'vue'
import { recapFileName, type RecapCard } from '@/helpers/recapHelpers'

const WIDTH = 1080
const HEIGHT = 1350
const PADDING = 72
const AMBER = '#ffb100'
const FONT = 'Rubik, sans-serif'

/** Draws the recap card: the site's dark stone background, amber accents, totals, awards and the top standings. */
async function drawRecapCard(card: RecapCard): Promise<Blob> {
  // The page loads Rubik; make sure the weights the card uses are ready before drawing with them.
  await Promise.all(['400', '700', '800'].map((w) => document.fonts?.load(`${w} 48px Rubik`).catch(() => undefined)))

  const canvas = document.createElement('canvas')
  canvas.width = WIDTH
  canvas.height = HEIGHT
  const ctx = canvas.getContext('2d')
  if (!ctx) throw new Error('Canvas is not available.')

  const background = ctx.createLinearGradient(0, 0, 0, HEIGHT)
  background.addColorStop(0, '#292524')
  background.addColorStop(1, '#13100e')
  ctx.fillStyle = background
  ctx.fillRect(0, 0, WIDTH, HEIGHT)

  const text = (value: string, x: number, y: number, size: number, weight: number, color: string, align: CanvasTextAlign = 'left') => {
    ctx.font = `${weight} ${size}px ${FONT}`
    ctx.fillStyle = color
    ctx.textAlign = align
    ctx.fillText(value, x, y, WIDTH - PADDING * 2)
  }

  // Header
  text('DRAFTPUCK', PADDING, 120, 64, 800, AMBER)
  text(card.heading, WIDTH - PADDING, 100, 34, 700, '#ffffff', 'right')
  text(card.subheading, WIDTH - PADDING, 140, 26, 400, '#a8a29e', 'right')

  // Totals
  const columnWidth = (WIDTH - PADDING * 2) / card.totals.length
  card.totals.forEach((total, i) => {
    const x = PADDING + columnWidth * i + columnWidth / 2
    text(total.label.toUpperCase(), x, 230, 24, 700, AMBER, 'center')
    text(String(total.value), x, 300, 64, 800, '#ffffff', 'center')
  })

  // Awards
  let y = 400
  text('AWARDS', PADDING, y, 26, 700, '#a8a29e')
  y += 24
  for (const award of card.awards) {
    ctx.fillStyle = '#1c1917'
    ctx.beginPath()
    ctx.roundRect(PADDING, y, WIDTH - PADDING * 2, 108, 20)
    ctx.fill()
    ctx.fillStyle = award.color
    ctx.fillRect(PADDING, y, 8, 108)
    text(award.title.toUpperCase(), PADDING + 36, y + 34, 22, 700, award.color)
    text(award.winner, PADDING + 36, y + 70, 34, 700, '#ffffff')
    text(award.detail, PADDING + 36, y + 97, 24, 400, '#d6d3d1')
    y += 120
  }

  // Standings
  if (card.standings.length > 0) {
    y += 30
    text('MOST DRINKS GIVEN', PADDING, y, 26, 700, '#a8a29e')
    card.standings.forEach((s, i) => {
      y += 56
      text(`${i + 1}. ${s.name}`, PADDING, y, 32, 700, '#ffffff')
      text(`${s.given} given · ${s.taken} taken`, WIDTH - PADDING, y, 28, 400, '#d6d3d1', 'right')
    })
  }

  text('draftpuck.com', WIDTH / 2, HEIGHT - 48, 26, 700, AMBER, 'center')

  return await new Promise<Blob>((resolve, reject) =>
    canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('Could not create the image.'))), 'image/png')
  )
}

/**
 * Shares the recap as an image: the phone's share sheet where it can share files (Android, iOS, some desktops),
 * otherwise a download.
 */
export function useRecapShare() {
  const isSharing = ref(false)

  async function share(card: RecapCard, joinCode: string): Promise<'shared' | 'downloaded' | 'cancelled'> {
    isSharing.value = true
    try {
      const blob = await drawRecapCard(card)
      const file = new File([blob], recapFileName(joinCode), { type: 'image/png' })

      if (navigator.canShare?.({ files: [file] })) {
        try {
          await navigator.share({ files: [file], title: `DraftPuck · ${card.heading}` })
          return 'shared'
        } catch (err) {
          // Closing the share sheet isn't an error worth reporting.
          if (err instanceof DOMException && err.name === 'AbortError') return 'cancelled'
          throw err
        }
      }

      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = file.name
      link.click()
      setTimeout(() => URL.revokeObjectURL(url), 10_000)
      return 'downloaded'
    } finally {
      isSharing.value = false
    }
  }

  return { isSharing, share }
}
