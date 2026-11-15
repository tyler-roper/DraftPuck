export function toQueryString<T extends object>(obj: T): string {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(obj)) {
    if (value !== undefined && value !== null) {
      params.append(key, String(value));
    }
  }

  const query = params.toString();
  return query ? `?${query}` : "";
}

/** The server's explanation of a failed request (ProblemDetails detail or title), or the fallback when there isn't one. */
export function getErrorMessage(error: unknown, fallback: string): string {
  const data = (error as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
  return data?.detail ?? data?.title ?? fallback
}
