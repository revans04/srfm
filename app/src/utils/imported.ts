export function splitImportedId(id: string): { docId: string; txId: string } {
  const parts = id.split('-');
  if (parts.length > 5) {
    return { docId: parts.slice(0, 5).join('-'), txId: parts.slice(5).join('-') };
  }
  return { docId: parts.slice(0, -1).join('-'), txId: parts[parts.length - 1] };
}
