// src/utils/helpers.ts
import type { Transaction } from '../types';
import { EntityType } from '../types';
import { v4 as uuidv4 } from 'uuid';
import { Timestamp } from 'firebase/firestore';

export function todayISO(date: Date = new Date()): string {
  const tzOffset = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - tzOffset).toISOString().slice(0, 10);
}

export function currentMonthISO(date: Date = new Date()): string {
  const tzOffset = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - tzOffset).toISOString().slice(0, 7);
}

/**
 * Formats a date string (YYYY-MM) or Date object to a localized string.
 * @param date - Date string (e.g., "2025-03") or Date object
 * @returns Formatted date string (e.g., "March 2025")
 */
export function formatDate(date: string | Date): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleDateString('en-US', {
    month: '2-digit',
    day: '2-digit',
    year: 'numeric',
  });
}

// Format a Timestamp to a readable string
export type TimestampLike = Timestamp | { seconds: number; nanoseconds: number } | string | Date | null | undefined;

export function formatTimestamp(timestamp: TimestampLike) {
  if (!timestamp) return 'Invalid timestamp';
  let date: Date | null = null;
  if (timestamp instanceof Timestamp) {
    date = timestamp.toDate();
  } else if (typeof timestamp === 'object' && 'seconds' in timestamp && 'nanoseconds' in timestamp) {
    date = new Timestamp(timestamp.seconds, timestamp.nanoseconds).toDate();
  } else if (typeof timestamp === 'string' || timestamp instanceof Date) {
    date = new Date(timestamp);
  }
  if (!date || isNaN(date.getTime())) return 'Invalid timestamp';
  return date.toLocaleDateString('en-US', { month: '2-digit', day: '2-digit', year: 'numeric' });
}

export function timestampToDate(t: Timestamp): Date {
  if (!t) return new Date();
  const milliseconds = t.seconds * 1000 + t.nanoseconds / 1e6;
  return new Date(milliseconds);
}

export function timestampToMillis(t: TimestampLike): number {
  if (!t) return 0;
  if (t instanceof Timestamp) return t.toMillis();
  if (typeof t === 'object' && 'seconds' in t && 'nanoseconds' in t) {
    const seconds = Number(t.seconds ?? 0);
    const nanos = Number(t.nanoseconds ?? 0);
    return seconds * 1000 + nanos / 1e6;
  }
  const d = new Date(t as string | number | Date);
  return isNaN(d.getTime()) ? 0 : d.getTime();
}

export function stringToFirestoreTimestamp(dateString: string): Timestamp {
  // Validate the date string format (YYYY-MM-DD)
  const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
  if (!dateRegex.test(dateString)) {
    throw new Error('Invalid date format. Expected YYYY-MM-DD');
  }

  // Parse the date string to a JavaScript Date
  const date = new Date(dateString);

  // Check if the date is valid
  if (isNaN(date.getTime())) {
    throw new Error('Invalid date string');
  }

  // Convert to Firestore Timestamp
  return Timestamp.fromDate(date);
}

/**
 * Formats a date string (YYYY-MM) or Date object to a localized string.
 * @param date - Date string (e.g., "2025-03") or Date object
 * @returns Formatted date string (e.g., "March 2025")
 */
export function formatDateMonthYYYY(date: string | Date): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleString('en-US', { month: 'long', year: 'numeric' });
}

/**
 * Formats a date string (YYYY-MM-DD) or Date object to MM/DD/YYYY without leading zeros.
 * @param date - Date string (e.g., "2025-03-09") or Date object
 * @returns Formatted date string (e.g., "3/9/2025")
 */
export function formatDateShort(date: string | Date): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  if (isNaN(d.getTime())) {
    throw new Error('Invalid date provided');
  }

  const month = d.getMonth() + 1; // getMonth() is 0-based, so add 1
  const day = d.getDate();
  const year = d.getFullYear();

  // Convert to numbers without leading zeros
  return `${month}/${day}/${year}`;
}

/**
 * Formats a date string (YYYY-MM-DD) or Date object to MM/DD/YYYY wit leading zeros.
 * @param date - Date string (e.g., "2025-03-09") or Date object
 * @returns Formatted date string (e.g., "03/09/2025")
 */
export function formatDateLong(dateStr: string): string {
  // Split the YYYY-MM-DD string
  const [year, month, day] = dateStr.split('-').map(Number);
  // Create a Date object in the local time zone (month is 0-based in JavaScript)
  const date = new Date(year, month - 1, day);
  // Format the date as MM/DD/YYYY
  return date.toLocaleDateString('en-US', {
    month: '2-digit',
    day: '2-digit',
    year: 'numeric',
  });
}

/**
 * Converts a date to the budget month format (YYYY-MM).
 * @param dateInput - The date to convert (can be a Date object, string, or number)
 * @returns A string in the format YYYY-MM (e.g., "2025-03")
 * @throws Error if the date is invalid
 */
export function toBudgetMonth(dateInput: Date | string | number): string {
  // Handle ISO-like strings directly to avoid timezone shifts
  if (typeof dateInput === 'string') {
    const match = dateInput.match(/^(\d{4})-(\d{2})/);
    if (match) {
      return `${match[1]}-${match[2]}`;
    }
  }

  const date = dateInput instanceof Date ? dateInput : new Date(dateInput);

  // Check if the date is valid
  if (isNaN(date.getTime())) {
    throw new Error('Invalid date: could not parse the input date');
  }

  // Extract year and month using UTC to avoid local timezone offsets
  const year = date.getUTCFullYear();
  const month = (date.getUTCMonth() + 1).toString().padStart(2, '0');

  return `${year}-${month}`;
}

/**
 * Formats a currency amount to a string with dollar sign and two decimal places.
 * @param amount - Number to format
 * @returns Formatted currency string (e.g., "$123.45")
 */
export function formatCurrency(amount: number | string): string {
  // Convert string to number, defaulting to 0 if invalid
  const numericAmount = typeof amount === 'string' ? parseFloat(amount) || 0 : amount;
  const safeAmount = isNaN(numericAmount) ? 0 : numericAmount;

  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(safeAmount);
}

export function getImportedTransactionDate(
  tx: { transactionDate?: string | null; postedDate?: string | null } | null | undefined,
): string {
  if (!tx) return '';
  return (tx.transactionDate || tx.postedDate || '').toString();
}

/**
 * Checks if a value is a valid number.
 * @param value - Value to check
 * @returns Boolean indicating if the value is a valid number
 */
export function isValidNumber(value: unknown): value is number {
  return typeof value === 'number' && !isNaN(value);
}

/**
 * Pads a number with leading zeros to a specified length.
 * @param num - Number to pad
 * @param length - Desired length
 * @returns Padded string
 */
export function padNumber(num: number, length: number): string {
  return num.toString().padStart(length, '0');
}

/**
 * Converts a dollar amount (e.g., 9.95) to cents (e.g., 995).
 * @param dollars - Amount in dollars (number or string)
 * @returns Amount in cents (integer)
 */
export function toCents(dollars: number | string): number {
  return Math.round(parseFloat(String(dollars)) * 100);
}

/**
 * Converts a cent amount (e.g., 995) to dollars with two decimal places (e.g., "9.95").
 * @param cents - Amount in cents
 * @returns Formatted dollar string
 */
export function toDollars(cents: number): string {
  return (cents / 100).toFixed(2);
}

/**
 * When importing amounts, sometimes they are positive and negative, sometimes they have $, sometimes they are in parenthesis.
 * @param value - Amount coming from the export
 * @returns positive or negative number
 */
export function parseAmount(value: string | null | undefined): number {
  if (!value || value.trim() === '') return 0;

  // Remove extra spaces, dollar signs, and commas
  let cleaned = value.trim().replace(/[\s,$]/g, '');

  // Handle parentheses for negative numbers (e.g., "($19.47)" -> "-19.47")
  const isNegative = cleaned.startsWith('(') && cleaned.endsWith(')');
  if (isNegative) {
    cleaned = cleaned.slice(1, -1); // Remove parentheses
  }

  // Parse to float and apply sign
  const parsed = parseFloat(cleaned);
  if (isNaN(parsed)) return 0; // Return 0 for invalid numbers
  return isNegative ? -parsed : parsed;
}

/** Recurring transaction helpers */
export function adjustTransactionDate(originalDate: string, newMonth: string, interval: Transaction['recurringInterval']): string {
  const original = new Date(originalDate);
  const [newYear, newMonthNum] = newMonth.split('-').map(Number);
  const newDate = new Date(newYear, newMonthNum - 1, 1); // Start of the new month

  if (interval === 'Daily') {
    // For daily, we’ll need to create a transaction for each day in the new month
    // For simplicity, return the same day for now; we'll handle daily recurrence separately
    newDate.setDate(original.getDate());
  } else if (interval === 'Weekly') {
    // Find the first occurrence of the same day of the week in the new month
    const originalDayOfWeek = original.getDay(); // 0 (Sunday) to 6 (Saturday)
    newDate.setDate(1); // Start at the 1st of the new month
    const firstDayOfWeek = newDate.getDay();
    const daysToAdd = (originalDayOfWeek - firstDayOfWeek + 7) % 7;
    newDate.setDate(1 + daysToAdd);
  } else {
    // Monthly, Quarterly, Bi-Annually, Yearly: Use the same day of the month, adjusted for month length
    const dayOfMonth = Math.min(original.getDate(), new Date(newYear, newMonthNum, 0).getDate());
    newDate.setDate(dayOfMonth);
  }

  return newDate.toISOString().slice(0, 10); // Format as YYYY-MM-DD
}

export function generateDailyTransactions(transaction: Transaction, newMonth: string): Transaction[] {
  const [newYear, newMonthNum] = newMonth.split('-').map(Number);
  const daysInMonth = new Date(newYear, newMonthNum, 0).getDate();
  const transactions: Transaction[] = [];

  for (let day = 1; day <= daysInMonth; day++) {
    const newDate = new Date(newYear, newMonthNum - 1, day);
    const { ...newTransaction } = transaction;
    newTransaction.id = uuidv4();
    transactions.push({
      ...newTransaction,
      date: newDate.toISOString().slice(0, 10),
      accountNumber: undefined,
      accountSource: undefined,
      postedDate: undefined,
      importedMerchant: undefined,
      status: undefined,
      budgetMonth: newMonth,
    });
  }

  return transactions;
}

export function generateBiWeeklyTransactions(transaction: Transaction, newMonth: string): Transaction[] {
  // Validate newMonth format (e.g., "2025-06")
  const parts = newMonth.split('-');
  if (parts.length !== 2) {
    throw new Error(`Invalid newMonth format: ${newMonth}. Expected YYYY-MM`);
  }
  const newYear = Number(parts[0]);
  const newMonthNum = Number(parts[1]);
  if (isNaN(newYear) || isNaN(newMonthNum)) {
    throw new Error(`Invalid year or month in newMonth: ${newMonth}`);
  }

  const transactions: Transaction[] = [];

  const originalDate = new Date(transaction.date);
  const originalDayOfWeek = originalDate.getDay(); // 0 (Sunday) to 6 (Saturday)

  // Start at the 1st of the new month
  const newDate = new Date(newYear, newMonthNum - 1, 1);
  const firstDayOfWeek = newDate.getDay();
  const daysToAdd = (originalDayOfWeek - firstDayOfWeek + 7) % 7;
  newDate.setDate(1 + daysToAdd);

  // Add a transaction for every two weeks in the month
  while (newDate.getMonth() === newMonthNum - 1) {
    transactions.push({
      id: uuidv4(),
      date: newDate.toISOString().slice(0, 10),
      merchant: transaction.merchant,
      categories: transaction.categories,
      amount: transaction.amount,
      notes: transaction.notes,
      recurring: transaction.recurring,
      recurringInterval: transaction.recurringInterval,
      userId: transaction.userId,
      isIncome: transaction.isIncome,
      budgetMonth: newMonth,
      accountNumber: transaction.accountNumber,
      accountSource: transaction.accountSource,
      postedDate: undefined,
      importedMerchant: undefined,
      status: undefined,
      checkNumber: transaction.checkNumber,
      deleted: transaction.deleted,
      entityId: transaction.entityId,
      taxMetadata: transaction.taxMetadata,
      receiptUrl: transaction.receiptUrl,
    });
    newDate.setDate(newDate.getDate() + 14); // Two weeks
  }

  return transactions;
}

export function generateWeeklyTransactions(transaction: Transaction, newMonth: string): Transaction[] {
  const [newYear, newMonthNum] = newMonth.split('-').map(Number);
  const transactions: Transaction[] = [];

  const originalDate = new Date(transaction.date);
  const originalDayOfWeek = originalDate.getDay(); // 0 (Sunday) to 6 (Saturday)

  // Start at the 1st of the new month
  const newDate = new Date(newYear, newMonthNum - 1, 1);
  const firstDayOfWeek = newDate.getDay();
  const daysToAdd = (originalDayOfWeek - firstDayOfWeek + 7) % 7;
  newDate.setDate(1 + daysToAdd);

  // Add a transaction for each week in the month
  while (newDate.getMonth() === newMonthNum - 1) {
    const { ...newTransaction } = transaction;
    newTransaction.id = uuidv4();
    transactions.push({
      ...newTransaction,
      date: newDate.toISOString().slice(0, 10),
      accountNumber: undefined,
      accountSource: undefined,
      postedDate: undefined,
      importedMerchant: undefined,
      status: undefined,
      budgetMonth: newMonth,
    });
    newDate.setDate(newDate.getDate() + 7); // Next week
  }

  return transactions;
}

export function formatEntityType(type: EntityType): string {
  switch (type) {
    case EntityType.Family:
      return 'Family';
    case EntityType.Business:
      return 'Business';
    case EntityType.RentalProperty:
      return 'Rental Property';
    case EntityType.Other:
      return 'Other';
    default:
      return type;
  }
}
