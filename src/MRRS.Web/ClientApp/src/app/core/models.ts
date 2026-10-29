export interface UserProfile {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  department: string | null;
  isActive: boolean;
  roles: string[];
}

export interface Room {
  id: number;
  name: string;
  building: string;
  floor: string;
  locationDetail: string | null;
  capacity: number;
  hasProjector: boolean;
  hasVideoConference: boolean;
  hasWhiteboard: boolean;
  hasConferencePhone: boolean;
  avNotes: string | null;
  isActive: boolean;
}

export interface RoomWrite {
  name: string;
  building: string;
  floor: string;
  locationDetail: string | null;
  capacity: number;
  hasProjector: boolean;
  hasVideoConference: boolean;
  hasWhiteboard: boolean;
  hasConferencePhone: boolean;
  avNotes: string | null;
  isActive: boolean;
}

export interface Booking {
  id: number;
  roomId: number;
  roomName: string;
  building: string;
  floor: string;
  locationDetail: string | null;
  organizerId: string;
  organizerName: string;
  title: string;
  notes: string | null;
  start: string;
  end: string;
  attendeeEmails: string | null;
  status: 'Confirmed' | 'Cancelled';
  canCancel: boolean;
}

export interface BookingWriteResult {
  booking: Booking;
  emailSent: boolean;
  emailWarning: string | null;
}

export interface UserWrite {
  userName: string;
  email: string;
  displayName: string;
  department: string | null;
  password: string | null;
  role: 'Admin' | 'Employee';
  isActive: boolean;
}

export function equipmentLabels(room: Room): string[] {
  const labels: string[] = [];
  if (room.hasProjector) {
    labels.push('Projector');
  }
  if (room.hasVideoConference) {
    labels.push('Video');
  }
  if (room.hasWhiteboard) {
    labels.push('Whiteboard');
  }
  if (room.hasConferencePhone) {
    labels.push('Phone');
  }
  return labels;
}

export function locationLine(room: Pick<Room, 'building' | 'floor' | 'locationDetail'>): string {
  const parts = [room.building, `Floor ${room.floor}`];
  if (room.locationDetail) {
    parts.push(room.locationDetail);
  }
  return parts.join(' · ');
}
