import { describe, expect, it } from 'vitest'
import { eventLogoFormError, eventLogoStatus, isLogoUrl, toCreateEventLogoRequest, type EventLogoForm } from '@/helpers/eventLogoHelpers'

const form = (overrides: Partial<EventLogoForm> = {}): EventLogoForm => ({
  name: 'Winter Classic',
  starts: '2026-12-31T18:00',
  ends: '2027-01-02T06:00',
  logoUrl: '/img/logo-wc.png',
  wideLogoUrl: 'https://example.com/wide.png',
  ...overrides
})

describe('eventLogoStatus', () => {
  const logo = { startsUtc: new Date('2026-12-31T00:00:00Z'), endsUtc: new Date('2027-01-02T00:00:00Z') }

  it('is upcoming before the start, on from the start, and ended from the end', () => {
    expect(eventLogoStatus(logo, new Date('2026-12-30T23:59:59Z'))).toBe('Upcoming')
    expect(eventLogoStatus(logo, logo.startsUtc)).toBe('On now')
    expect(eventLogoStatus(logo, logo.endsUtc)).toBe('Ended')
  })
})

describe('isLogoUrl', () => {
  it('takes site paths and web URLs', () => {
    expect(isLogoUrl('/img/logo-wc.png')).toBe(true)
    expect(isLogoUrl(' https://example.com/a.png ')).toBe(true)
    expect(isLogoUrl('http://example.com/a.png')).toBe(true)
  })

  it('refuses anything else', () => {
    expect(isLogoUrl('')).toBe(false)
    expect(isLogoUrl('logo.png')).toBe(false)
    expect(isLogoUrl('//elsewhere.com/a.png')).toBe(false)
    expect(isLogoUrl('javascript:alert(1)')).toBe(false)
  })
})

describe('eventLogoFormError', () => {
  it('is null for a good form', () => {
    expect(eventLogoFormError(form())).toBeNull()
  })

  it('names the first problem', () => {
    expect(eventLogoFormError(form({ name: '  ' }))).toMatch(/name/)
    expect(eventLogoFormError(form({ ends: '' }))).toMatch(/starts and ends/)
    expect(eventLogoFormError(form({ ends: form().starts }))).toMatch(/end after/)
    expect(eventLogoFormError(form({ logoUrl: 'logo.png' }))).toMatch(/^The logo/)
    expect(eventLogoFormError(form({ wideLogoUrl: '' }))).toMatch(/^The wide logo/)
  })
})

describe('toCreateEventLogoRequest', () => {
  it('trims, and turns the local times into UTC', () => {
    const request = toCreateEventLogoRequest(form({ name: ' Winter Classic ', logoUrl: ' /img/logo-wc.png ' }))

    expect(request.name).toBe('Winter Classic')
    expect(request.logoUrl).toBe('/img/logo-wc.png')
    expect(request.startsUtc).toBe(new Date(2026, 11, 31, 18, 0).toISOString())
    expect(request.endsUtc).toBe(new Date(2027, 0, 2, 6, 0).toISOString())
  })
})
