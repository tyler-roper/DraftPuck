import { helpers } from '@vuelidate/validators'

export function uniqueInArray(arrayGetter: () => any[]) {
  return helpers.withMessage('Value must be unique', (value) => {
    if (value == null || value === '') return true // Don't validate empty values (let required handle it)
    const arr = arrayGetter()
    if (!Array.isArray(arr)) return true
    const count = arr.filter((item) => item === value).length
    return count <= 1
  })
}

export function nickname(value?: string) {
  if (!value) return false
  if (value.trim().length === 0) return false
  if (value.length > 25) return false

  const regex = new RegExp(`^[A-Za-z0-9][A-Za-z0-9 '_]*$`, 'i')
  if (!regex.test(value)) return false

  return true
}

export function password(value?: string) {
  if (!value) return false
  if (value.trim().length === 0) return false
  if (value.length > 25 || value.length < 8) return false

  return true
}

export function optionalPassword(value?: string) {
  if (!value || value.trim().length === 0) return true
  if (value.length > 25 || value.length < 8) return false
  return true
}
