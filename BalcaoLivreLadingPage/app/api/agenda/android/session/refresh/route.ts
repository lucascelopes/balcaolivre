import {
  agendaAndroidErrorResponse,
  agendaAndroidOptionsResponse,
  refreshAgendaAndroidSession,
} from "../../../../../lib/agenda-android-server";

export async function POST(request: Request) {
  try {
    return await refreshAgendaAndroidSession(request);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAndroidOptionsResponse("POST, OPTIONS");
}
