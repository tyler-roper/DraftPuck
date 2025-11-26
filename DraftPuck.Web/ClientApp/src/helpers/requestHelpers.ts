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