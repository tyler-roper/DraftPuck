import { describe, expect, it } from 'vitest'
import { nickname, optionalPassword, password } from '@/helpers/validationHelpers'

describe('nickname', () => {
  it.each(['Sam', 'sam_99', "O'Reilly", 'Big Sam'])('accepts %s', (value) => {
    expect(nickname(value)).toBe(true)
  })

  it.each([undefined, '', '   ', '_leading', '<script>', 'a'.repeat(26)])('rejects %s', (value) => {
    expect(nickname(value)).toBe(false)
  })
})

describe('password', () => {
  it('requires 8 to 25 characters', () => {
    expect(password('1234567')).toBe(false)
    expect(password('12345678')).toBe(true)
    expect(password('a'.repeat(25))).toBe(true)
    expect(password('a'.repeat(26))).toBe(false)
    expect(password(undefined)).toBe(false)
  })

  it('does not count surrounding spaces toward the minimum, like the server', () => {
    expect(password('   abcde   ')).toBe(false)
    expect(optionalPassword('   abcde   ')).toBe(false)
  })
})

describe('optionalPassword', () => {
  it('allows leaving the password blank but validates anything typed', () => {
    expect(optionalPassword(undefined)).toBe(true)
    expect(optionalPassword('')).toBe(true)
    expect(optionalPassword('short')).toBe(false)
    expect(optionalPassword('long-enough')).toBe(true)
  })
})
