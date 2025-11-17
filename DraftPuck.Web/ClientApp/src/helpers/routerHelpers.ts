import type { RouteParams } from 'vue-router'

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
