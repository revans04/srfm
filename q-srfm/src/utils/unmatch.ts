/**
 * Build the API path for unmatching an imported (bank) transaction.
 *
 * The imported transaction id is a composite TEXT id of the shape
 * "{documentId}-{transactionId}" (both UUIDs), so it contains dashes and could
 * in principle contain other URL-significant characters. It MUST be encoded as
 * a single path segment, or the backend will resolve the wrong route / id.
 *
 * Returns a path relative to the API base (no leading slash), e.g.
 * `budget/imported-transactions/<enc>/unmatch`.
 */
export function buildUnmatchImportedPath(importedTransactionId: string): string {
  return `budget/imported-transactions/${encodeURIComponent(importedTransactionId)}/unmatch`;
}
