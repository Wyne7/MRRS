import { HttpErrorResponse } from '@angular/common/http';

export function pad(value: number): string {
  return String(value).padStart(2, '0');
}

export function dateInput(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function todayInput(): string {
  return dateInput(new Date());
}

export function addDays(isoDate: string, days: number): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  const date = new Date(year || 1970, (month || 1) - 1, day || 1);
  date.setDate(date.getDate() + days);
  return dateInput(date);
}

export function longDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return new Date(year || 1970, (month || 1) - 1, day || 1).toLocaleDateString(undefined, {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  });
}

export function clock(hour: number, minute: number): string {
  const suffix = hour >= 12 ? 'PM' : 'AM';
  const hour12 = hour % 12 || 12;
  return `${hour12}:${pad(minute)} ${suffix}`;
}

export function formatTime(value: string): string {
  const match = /(?:T|^)(\d{2}):(\d{2})/.exec(value);
  if (!match) {
    return value;
  }
  return clock(Number(match[1]), Number(match[2]));
}

export function minutesOf(iso: string): number {
  const match = /T(\d{2}):(\d{2})/.exec(iso);
  if (!match) {
    return 0;
  }
  return Number(match[1]) * 60 + Number(match[2]);
}

export function quarterHours(startHour = 7, endHour = 19): string[] {
  const times: string[] = [];
  for (let hour = startHour; hour <= endHour; hour += 1) {
    for (const minute of [0, 15, 30, 45]) {
      if (hour === endHour && minute > 0) {
        break;
      }
      times.push(`${pad(hour)}:${pad(minute)}`);
    }
  }
  return times;
}

export function apiMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { message?: string } | string | null;
    if (body && typeof body === 'object' && typeof body.message === 'string' && body.message.length > 0) {
      return body.message;
    }
    if (typeof body === 'string' && body.length > 0 && body.length < 300) {
      return body;
    }
  }
  return fallback;
}

export function reservationNote(result: { emailSent: boolean; emailWarning: string | null }, verb: 'Reserved' | 'Cancelled'): string {
  if (result.emailSent) {
    return `${verb}. Invitation email sent.`;
  }
  return `${verb}. ${result.emailWarning ?? 'No invitation email was sent.'}`;
}
