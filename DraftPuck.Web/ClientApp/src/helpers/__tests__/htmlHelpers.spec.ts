import { describe, expect, it } from 'vitest'
import { escapeHtml, escapeRegExp } from '@/helpers/htmlHelpers'

describe('escapeHtml', () => {
  it('escapes every character that can open markup or break out of an attribute', () => {
    expect(escapeHtml(`<img src=x onerror="alert('x')">&`)).toBe(
      '&lt;img src=x onerror=&quot;alert(&#039;x&#039;)&quot;&gt;&amp;'
    )
  })

  it('leaves ordinary names alone', () => {
    expect(escapeHtml("Connor McDavid")).toBe('Connor McDavid')
  })

  it('escapes ampersands first, so existing entities are not left unescaped', () => {
    expect(escapeHtml('&lt;')).toBe('&amp;lt;')
  })
})

describe('escapeRegExp', () => {
  it('makes names match literally inside a pattern', () => {
    const name = 'a.b*(c)'
    const pattern = new RegExp(escapeRegExp(name))

    expect(pattern.test('a.b*(c)')).toBe(true)
    expect(pattern.test('aXbbbc')).toBe(false)
  })
})
