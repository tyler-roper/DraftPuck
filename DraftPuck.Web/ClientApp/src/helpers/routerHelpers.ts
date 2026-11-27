import { START_LOCATION, type RouteLocationNormalized, type RouteParams } from 'vue-router'

let isOnFirstPage = true

/**
 * Call after each successful navigation. Records whether the page now showing is the one the app opened on (a launch,
 * a reload, a link) rather than one reached from another page.
 */
export function recordNavigation(from: RouteLocationNormalized) {
  isOnFirstPage = from === START_LOCATION
}

/** Whether the page showing is the one the app opened on (see recordNavigation). */
export function isShowingFirstPage(): boolean {
  return isOnFirstPage
}

export function setPageMetaTags(metaTags: MetaTag[], params: RouteParams = {}) {
  metaTags.forEach((tag) => {
    const firstKey = Object.keys(tag)[0]
    const element = document.querySelector(`meta[${firstKey}='${tag[firstKey as keyof typeof tag]}']`)

    if (!element) return
    element.setAttribute('content', replaceWithParams(tag.content, params))
  })
}

export function setPageTitle(title: string | undefined, params: RouteParams = {}) {
  const titleElement = document.querySelector('title')
  if (!titleElement) return

  titleElement.innerHTML = title ? replaceWithParams(title, params) : 'DRAFTPUCK - A live hockey drinking game.'
}

function replaceWithParams(str: string, params: RouteParams) {
  Object.entries(params).forEach(([k, v]) => {
    str = str.replace(`{{${k}}}`, v as string)
  })

  return str
}
